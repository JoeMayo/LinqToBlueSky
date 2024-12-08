namespace LinqToBlueSky.OAuth;

public class BlueSkySession
{
    public string? Did { get; set; }
    public Diddoc? DidDoc { get; set; }
    public string? Handle { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool EmailAuthFactor { get; set; }
    public string? AccessJwt { get; set; }
    public string? RefreshJwt { get; set; }
    public bool Active { get; set; }
}

public class Diddoc
{
    public string[]? Context { get; set; }
    public string? Id { get; set; }
    public string[]? AlsoKnownAs { get; set; }
    public Verificationmethod[]? VerificationMethod { get; set; }
    public Service[]? Service { get; set; }
}

public class Verificationmethod
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Controller { get; set; }
    public string? PublicKeyMultibase { get; set; }
}

public class Service
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? ServiceEndpoint { get; set; }
}
