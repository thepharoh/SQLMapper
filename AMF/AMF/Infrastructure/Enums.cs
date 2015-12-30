namespace AMF.Infrastructure
{
    public enum LoginResult
    {
        Success,
        Failed,
        Disabled,
        Unknown
    }

    public enum MethodResult
    {
        Success,
        Fail,
        Exception,
        Disallowed,
        UniqueValueFail,
        NotExist
    }

    public enum ExclusionTypes
    {
        CUD,
        Select,
        Both
    }

    public class Enums
    {
    }
}