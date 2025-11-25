---
applyTo: "**/Controllers/**/*.cs"
---

# API Controller Guidelines

## Critical Rule: NO Business Logic

Controllers should ONLY:
1. Map DTOs to use case inputs
2. Call use cases
3. Map use case results to response DTOs
4. Handle HTTP concerns (routing, status codes, auth)

## Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class EntityController : ControllerBase
{
    private readonly ICreateEntityUseCase _useCase;

    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Create([FromBody] RequestDto request)
    {
        var input = new UseCaseInput { /* map from request */ };
        var result = await _useCase.ExecuteAsync(input);
        
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });
            
        return Ok(result.Data);
    }
}
```

## Routing Rules

- `/api/*` - User operations on their own resources
- `/adminapi/*` - System-level operations (require admin role)

## Security

- Use `[Authorize]` for protected endpoints
- Use `[Authorize(Roles = "Admin")]` for admin endpoints
- Validate all input with data annotations
- Never expose internal exceptions to clients
