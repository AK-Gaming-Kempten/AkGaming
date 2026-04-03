namespace AkGaming.Core.Constants;

public static class ClubConstants
{
    public static class Organization
    {
        public const string ShortName = "AK Gaming";
        public const string LegalName = "AK Gaming e.V.";
        public const string RegisterNumber = "VR 201431";
        public const string RegisterCourt = "Amtsgericht Kempten (Allgäu)";
    }

    public static class Address
    {
        public const string Street = "Bahnhofstraße 61";
        public const string PostalCode = "87435";
        public const string City = "Kempten";
        public const string CityWithRegion = "Kempten (Allgäu)";
    }

    public static class EmailAddresses
    {
        public const string Info = "info@akgaming.de";
        public const string Board = "vorstand@akgaming.de";
        public const string Esports = "esport@akgaming.de";
        public const string SocialMedia = "socialmedia@akgaming.de";
        public const string Finance = "finanzen@akgaming.de";
        public const string Identity = "identity@akgaming.de";
        public const string NoReply = "no-reply@akgaming.de";
    }

    public static class Urls
    {
        public const string Website = "https://akgaming.de";
        public const string PrivacyPolicy = Website + "/datenschutz";
        public const string MembershipFees = Website + "/mitgliedschaft/mitgliedsbeitrag";
        public const string ArticlesOfAssociation = Website + "/Vereinssatzung-AK-Gaming-e.V..pdf";
        public const string MembershipFeeRegulations = Website + "/Beitragsordnung-AK-Gaming-e.V..pdf";
        public const string LogoAsset = Website + "/assets/akgaming_logo.png";
        public const string DiscordInvite = "https://discord.com/invite/5J5uJKJAhT";

        public const string ManagementBase = "https://management.akgaming.de";
        public const string ManagementMembership = ManagementBase + "/membership/";
        public const string ManagementMemberRequests = ManagementBase + "/member-management/requests";

        public const string IdentityBase = "https://identity.akgaming.de";
    }

    public static class BankAccount
    {
        public const string Iban = "DE59 7336 9920 0000 8872 85";
        public const string Bic = "GENODEF1SFO";
        public const string Blz = "7336 9920";
        public static readonly string AccountHolder = Organization.LegalName;
    }

    public static class Contacts
    {
        public static readonly ClubContact FirstChair = new("Kai Höft", "1. Vorstand", EmailAddresses.Board);
        public static readonly ClubContact SecondChair = new("Colin Gaiser", "2. Vorstand", EmailAddresses.Board);
        public static readonly ClubContact ThirdChair = new("Stefan Oswald", "3. Vorstand", EmailAddresses.Board);
        public static readonly ClubContact Treasurer = new("Jan-Gustav Liedke", "Kassenwart", EmailAddresses.Finance);
        public static readonly ClubContact Esports = new("Hannah Martin", "Ansprechpartner Abteilung ESports", EmailAddresses.Esports);
        public static readonly ClubContact SocialMedia = new("Lennart Hartmann", "Ansprechpartner Social Media", EmailAddresses.SocialMedia);

        public static IReadOnlyList<ClubContact> BoardMembers { get; } =
        [
            FirstChair,
            SecondChair,
            ThirdChair
        ];
    }
}

public sealed record ClubContact(string Name, string Role, string Email);
