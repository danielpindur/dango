using Example.Core.Generated.Dango.Mappings;

namespace Example.Core;
using ApiUserType = Example.Core.Api.UserType;

public class TestClass
{
    public void TestMethod()
    {
        var apiType = ApiUserType.AdminUser;
        var localType = apiType.ToUserType();
    }
}