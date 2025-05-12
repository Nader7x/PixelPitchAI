using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Auth.Queries;

public class GetUserProfileQuery : IRequest<GetUserProfileQueryResponse>
{
    public string UserId { get; set; }
}

public class GetUserProfileQueryResponse
{
    public bool Succeeded { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int? FavoriteTeamId { get; set; }
    public string FavoriteTeamName { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }
    public string[] Roles { get; set; }
    public string Error { get; set; }
}

public class GetUserProfileQueryHandler(IApplicationUserRepository userRepository)
    : IRequestHandler<GetUserProfileQuery, GetUserProfileQueryResponse>
{
    public async Task<GetUserProfileQueryResponse> Handle(GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user by id
            var user = await userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new GetUserProfileQueryResponse
                {
                    Succeeded = false,
                    Error = "User not found"
                };
            }

            // Get user roles
            var roles = await userRepository.GetUserRolesAsync(user);

            // Return user profile
            return new GetUserProfileQueryResponse
            {
                Succeeded = true,
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FavoriteTeamId = user.FavoriteTeamId,
                FavoriteTeamName = user.FavoriteTeam?.Name,
                Created = user.Created,
                LastLogin = user.LastLogin,
                Roles = roles.ToArray()
            };
        }
        catch (Exception ex)
        {
            return new GetUserProfileQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}