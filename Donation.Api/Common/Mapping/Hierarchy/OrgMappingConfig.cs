﻿using Donation.Application.Simple;
using Donation.Contracts.Simple;
using Donation.Domain.HierarchyAggregate;
using Donation.Domain.SimpleAggregates;
using Mapster;
namespace Donation.Api.Common.Mapping.Hierarchy
{
  public class OrgMappingConfig : IRegister
  {
    public void Register(TypeAdapterConfig config)
    {
      config.NewConfig<SimpleRequestCreate, SimpleCommandCreate<Org>>()
        .Map(dest => dest, src => src);

      config.NewConfig<Guid, SimpleQueryGetById<Org>>()
       .Map(dest => dest.Id, src => SimpleValueObject.Create(src));

      config.NewConfig<Org, SimpleResponseCreate>()
        .Map(dest => dest.Id, src => src.Id.Value);

    }
  }
}
