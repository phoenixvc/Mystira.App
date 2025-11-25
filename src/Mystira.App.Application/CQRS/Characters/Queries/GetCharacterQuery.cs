using Mystira.App.Domain.Models;
using Mystira.App.Application.Interfaces;

namespace Mystira.App.Application.CQRS.Characters.Queries;

/// <summary>
/// Query to get a specific character by ID from the character map.
/// </summary>
public record GetCharacterQuery(string CharacterId) : IQuery<Character?>;
