### Menu Request Record
```csharp
namespace Donation.Contracts.Menus
{
  public record CreateMenuRequest(
    string hostId,
    string Name,
    string Description,
    List<MenuSection> Sections);
  
  public record MenuSection(
    string Name,
    string Description,
    List<MenuItem > Items);

  public record MenuItem(
    string Name, 
    string Description);
}
```
### Menu Response
```csharp
namespace Donation.Contracts.Menus
{
  public record MenuResponse(
    Guid Id,
    string Name,
    string Description,
    float? AverageRating,
    List<MenuSectionResponse> Sections,
    string HostId,
    List<string> DinnerIds,
    List<string> MenuReviewIds,
    DateTime CreatedDateTime,
    DateTime UpdatedDateTime);

  public record MenuSectionResponse(
    string Id,
    string Name,
    string Description,
    List<MenuItemResponse> Items);

  public record MenuItemResponse(
    string Id,
    string Name,
    string Description);
}
```
### Menu Create Command
```csharp
namespace Donation.Application.Menus.Commands.CreateMenu
{
  public record CreateMenuCommand(
    string HostId,
    string Name,
    string Description,
    List<MenuSectionCommand> Sections) : IRequest<ErrorOr<Menu>>;

  public record MenuSectionCommand(
    string Name,
    string Description,
    List<MenuItemCommand> Items);

  public record MenuItemCommand(
    string Name,
    string Description);
}
```
### Menu Mapping
```csharp
namespace Donation.Api.Common.Mapping
{
  public class MenuMappingConfig : IRegister
  {
    public void Register(TypeAdapterConfig config)
    {
      config.NewConfig<
        (CreateMenuRequest Request, string HostId),  // src area
        CreateMenuCommand>() // dest area
        .Map(dest => dest.HostId, src => src.HostId)
        .Map(dest => dest, src => src.Request);

      // Configuration of Mapping MenuResponse to Menu (dest is Menu, src is MenuResponse)
      // There is better way of rewriting it
      config.NewConfig<Menu, MenuResponse>()
        .Map(dest => dest.Id, src => src.Id.Value)
        .Map(dest => dest.AverageRating, src => src.AverageRating.Value)
        .Map(dest => dest.HostId, src => src.HostId.Value)
        .Map(dest => dest.DinnerIds, src => src.DinnerIds.Select(dinnerId => dinnerId.Value))
        .Map(dest => dest.MenuReviewIds, src => src.MenuReviewIds.Select(reviewId => reviewId.Value));

      config.NewConfig<MenuSection, MenuSectionResponse>()
        .Map(dest => dest.Id, src => src.Id.Value);

      config.NewConfig<MenuItem, MenuItemResponse>()
        .Map(dest => dest.Id, src => src.Id.Value);

    }
  }
}
```
### DB Context
```csharp
namespace Donation.Infrastructure.Persistence
{
  public class DonationDbContext : DbContext
  {
    public DonationDbContext(DbContextOptions<DonationDbContext> options) : base(options) { }
    
    public DbSet<Menu> Menus { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(DonationDbContext).Assembly
      );
      base.OnModelCreating(modelBuilder);
    }
  }
}
```
### IMenu Repo
```csharp
namespace Donation.Application.Common.Persistence
{
  public interface IMenuRepository
  {
    void Add(Menu menu);
  }
}
```
### Menu Repo
```csharp
namespace Donation.Infrastructure.Persistence.Repositories
{
  public class MenuRepository : IMenuRepository
  {
    private readonly DonationDbContext dbContext;

    public MenuRepository(DonationDbContext dbContext)
    {
      this.dbContext = dbContext;
    }
    public void Add(Menu menu)
    {
      //dbContext.Menus.Add(menu);
      dbContext.Add(menu);
      dbContext.SaveChanges();
    }
  }
}
```
### Create Command Hanndler
```csharp
namespace Donation.Application.Menus.Commands.CreateMenu
{
  public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, ErrorOr<Menu>>
  {
    private readonly IMenuRepository _menuRepository;

    public CreateMenuCommandHandler(IMenuRepository menuRepository)
    {
      _menuRepository = menuRepository;
    }

    public async Task<ErrorOr<Menu>> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
      await Task.CompletedTask;
      // 1. Create Menu
      var menu = Menu.Create(
          hostId: HostId.CreateUnique(),//HostId.Create(request.HostId),
          name: request.Name,
          description: request.Description,
          sections: request.Sections.ConvertAll(sections => MenuSection.Create(
              name: sections.Name,
              description: sections.Description,
              items: sections.Items.ConvertAll(items => MenuItem.Create(
                  name: items.Name,
                  description: items.Description)))));
      // 2. Persist Menu
      _menuRepository.Add(menu);
      // 3. Return Menu
      return menu;
    }
  }
}
```
### Menu Create Command Validator
```csharp
namespace Donation.Application.Menus.Commands.CreateMenu
{
  public class CreateMenuCommandValidator : AbstractValidator<CreateMenuCommand>
  {
    public CreateMenuCommandValidator() {
      RuleFor(x => x.Name).NotEmpty();
      RuleFor(x => x.Description).NotEmpty();
      RuleFor(x => x.Sections).NotEmpty();
    }
  }
}
```

### Controller Action
```csharp
namespace Donation.Api.Controllers
{
  [Route("hosts/{hostId}/menus")]
  public class MenusController : ApiController
  {
    private readonly IMapper mapper;
    private readonly ISender mediator;
    public MenusController(IMapper mapper, ISender mediator)
    {
      this.mapper = mapper;
      this.mediator = mediator;
    }
    [HttpPost]
    public async Task<IActionResult> CreateMenu(CreateMenuRequest request, string hostId)
    {
      var command = mapper.Map<CreateMenuCommand>((request, hostId));
      var createMenuResult = await mediator.Send(command);
      return createMenuResult.Match(
        menu => Ok(mapper.Map<MenuResponse>(menu)),
        errors => Problem(errors)
      );
    }
  }
}
```
