using Dango.Abstractions;
using ApiUserType = Example.Core.Api.UserType;

namespace Example.Core;

public class MappingRegistrar : IDangoMapperRegistrar
{
    public void Register(IDangoMapperRegistry registry)
    {
        registry.Enum<ApiUserType, UserType>();
    }
}