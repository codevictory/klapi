using Microsoft.Data.Sqlite;

public static class UserEndpoints
{
  public static void MapUserEndpoints(WebApplication app)
  {
    app.MapGet("/users", async (HttpContext httpContext) =>
    {
      var userService = httpContext.RequestServices.GetRequiredService<UserService>();
      var users = await userService.GetUsers();
      await httpContext.Response.WriteAsJsonAsync(users);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("AdminOnly")
    .WithName("GetUsers");

    app.MapPost("/users", async (HttpContext httpContext) =>
    {
      var userService = httpContext.RequestServices.GetRequiredService<UserService>();
      var request = await httpContext.Request.ReadFromJsonAsync<AppUserCreateRequest>();

      if (request is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body is required."
        });
        return;
      }

      if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.DisplayName))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Username, password and display name are required."
        });
        return;
      }

      try
      {
        var createdUser = await userService.CreateUser(request);
        httpContext.Response.StatusCode = StatusCodes.Status201Created;
        httpContext.Response.Headers.Location = $"/users/{createdUser.Username}";
        await httpContext.Response.WriteAsJsonAsync(createdUser);
      }
      catch (SqliteException)
      {
        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "User with the same username already exists."
        });
      }
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("AdminOnly")
    .WithName("CreateUser");

    app.MapPut("/users/{username}", async (HttpContext httpContext, string username) =>
    {
      var userService = httpContext.RequestServices.GetRequiredService<UserService>();
      var request = await httpContext.Request.ReadFromJsonAsync<AppUserUpdateRequest>();

      if (request is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body is required."
        });
        return;
      }

      if (string.IsNullOrWhiteSpace(request.DisplayName))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Display name is required."
        });
        return;
      }

      var normalizedTargetUsername = username.Trim().ToLowerInvariant();
      var existingUser = await userService.GetUser(normalizedTargetUsername);

      if (existingUser is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "User not found."
        });
        return;
      }

      var adminCount = await userService.GetAdminCount();

      if (existingUser.IsAdmin && !request.IsAdmin && adminCount <= 1)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Cannot remove admin role from the last admin user."
        });
        return;
      }

      var updatedUser = await userService.UpdateUser(username, request);

      if (updatedUser is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "User not found."
        });
        return;
      }

      await httpContext.Response.WriteAsJsonAsync(updatedUser);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("AdminOnly")
    .WithName("UpdateUser");

    app.MapDelete("/users/{username}", async (HttpContext httpContext, string username) =>
    {
      var userService = httpContext.RequestServices.GetRequiredService<UserService>();
      var currentUsername = httpContext.User.Identity?.Name?.Trim().ToLowerInvariant() ?? string.Empty;
      var normalizedTargetUsername = username.Trim().ToLowerInvariant();

      var existingUser = await userService.GetUser(normalizedTargetUsername);
      if (existingUser is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "User not found."
        });
        return;
      }

      if (currentUsername == normalizedTargetUsername)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "You cannot delete your own user account."
        });
        return;
      }

      var adminCount = await userService.GetAdminCount();
      if (existingUser.IsAdmin && adminCount <= 1)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Cannot delete the last admin user."
        });
        return;
      }

      var deleted = await userService.DeleteUser(username);

      if (!deleted)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "User not found."
        });
        return;
      }

      httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("AdminOnly")
    .WithName("DeleteUser");
  }
}
