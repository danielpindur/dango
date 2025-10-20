using Tofu.Abstractions;

namespace Test.Namespace
{
    public enum SourceEnum
    {
        A,
        B
    }

    public enum DestinationEnum
    {
        C,
        D
    }

    public class MyRegistrar : ITofuMapperRegistrar
    {
        public void Register(ITofuMapperRegistry registry)
        {
            registry.Enum<SourceEnum, DestinationEnum>()
                .MapByValue()
                .WithOverrides(new Dictionary<SourceEnum, DestinationEnum>
                {
                    { SourceEnum.A, DestinationEnum.D },
                    { SourceEnum.B, DestinationEnum.C },
                });
        }
    }
}