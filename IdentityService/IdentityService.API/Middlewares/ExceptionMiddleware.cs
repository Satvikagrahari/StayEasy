using System.Net;
using System.Text.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationException ex)
        {
            await HandleException(context, ex.Message, HttpStatusCode.BadRequest);
        }
        catch (Exception)
        {
            await HandleException(context, "Internal Server Error", HttpStatusCode.InternalServerError);
        }
    }

    private static async Task HandleException(HttpContext context, string message, HttpStatusCode statusCode)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear(); //  IMPORTANT
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = message
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}