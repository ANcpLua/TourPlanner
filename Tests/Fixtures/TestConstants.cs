namespace Tests.Fixtures;

public static class TestConstants
{
    public const string TestUserId = "test-user-id-12345";
    public const string ValidSearchText = "Sample Tour";
    public const string InvalidSearchText = "NonexistentTour";
    public static readonly Guid TestGuid = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NonexistentGuid = new("99999999-9999-9999-9999-999999999999");
    public static readonly DateTime TestDateTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly (double Latitude, double Longitude) TestCoordinates = (48.2082, 16.3738);
}
