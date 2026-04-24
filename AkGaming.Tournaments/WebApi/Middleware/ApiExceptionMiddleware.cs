using AkGaming.Tournaments.Application.Exceptions;

namespace AkGaming.Tournaments.WebApi.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (ForbiddenException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, exception.Message);
        }
        catch (NotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (ConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        return Results.Problem(statusCode: statusCode, detail: detail).ExecuteAsync(context);
    }
}
