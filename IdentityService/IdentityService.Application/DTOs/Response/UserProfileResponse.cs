namespace IdentityService.Application.DTOs.Response
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }
}
