
using Domain.Models;
using MediatR;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Teams.Commands;

public class CreateTeamCommand : IRequest<CreateTeamCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string ShortName { get; set; }
    
    [StringLength(500)]
    public string Logo { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Country { get; set; }
    
    [Required]
    [StringLength(100)]
    public string City { get; set; }
    
    [Required]
    [StringLength(50)]
    public string League { get; set; }
    
    public DateTime FoundationDate { get; set; }
    
    [StringLength(20)]
    public string PrimaryColor { get; set; }
    
    [StringLength(20)]
    public string SecondaryColor { get; set; }
    
    public int? StadiumId { get; set; }
}

public class CreateTeamCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Error { get; set; }
}

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, CreateTeamCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateTeamCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<CreateTeamCommandResponse> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if team with the same name already exists
            var existingTeam = await _unitOfWork.Teams.GetByNameAsync(request.Name);
            if (existingTeam != null)
            {
                return new CreateTeamCommandResponse
                {
                    Succeeded = false,
                    Error = $"Team with name '{request.Name}' already exists"
                };
            }
            
            // Create new team
            var team = new Team
            {
                Name = request.Name,
                ShortName = request.ShortName,
                Logo = request.Logo,
                Country = request.Country,
                City = request.City,
                League = request.League,
                FoundationDate = request.FoundationDate,
                PrimaryColor = request.PrimaryColor,
                SecondaryColor = request.SecondaryColor,
                StadiumId = request.StadiumId
            };
            
            await _unitOfWork.Teams.AddAsync(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new CreateTeamCommandResponse
            {
                Succeeded = true,
                Id = team.Id,
                Name = team.Name
            };
        }
        catch (Exception ex)
        {
            return new CreateTeamCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
