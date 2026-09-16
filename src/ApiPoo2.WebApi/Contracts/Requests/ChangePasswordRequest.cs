namespace ApiPoo2.WebApi.Contracts.Requests;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);