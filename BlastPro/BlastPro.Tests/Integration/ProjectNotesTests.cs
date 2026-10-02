using System.Net;
using System.Net.Http.Json;
using BlastPro.Api.Models.Dtos;
using BlastPro.Tests.Support;

namespace BlastPro.Tests.Integration;

public sealed class ProjectNotesTests
{
    [Fact]
    public async Task Project_notes_comments_and_photos_sync_through_the_api_with_company_boundaries()
    {
        await using var host = new ApiTestHost();
        using var owner = host.Client();
        using var outsider = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, owner);
        await CompanyTestSetup.CreateMainAsync(host, outsider);
        var projectResponse = await owner.PostAsJsonAsync("/api/projects",
            new { name = "Mobile site", siteLocation = "Site", blastType = "Surface" });
        projectResponse.EnsureSuccessStatusCode();
        var projectId = (await projectResponse.Content.ReadFromJsonAsync<ProjectDetailDto>())!.Id;

        var create = await owner.PostAsJsonAsync($"/api/projects/{projectId}/notes",
            new { title = "Bench", body = "Check the collar" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var note = (await create.Content.ReadFromJsonAsync<ProjectNoteDto>())!;
        Assert.Equal("Check the collar", Assert.Single((await owner.GetFromJsonAsync<List<ProjectNoteDto>>(
            $"/api/projects/{projectId}/notes"))!).Body);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/projects/{projectId}/notes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PutAsJsonAsync($"/api/projects/{projectId}/notes/{note.Id}",
            new { title = "Changed", body = "Wrong company" })).StatusCode);

        var comment = await owner.PostAsJsonAsync($"/api/projects/{projectId}/notes/{note.Id}/comments",
            new { body = "Photograph attached" });
        Assert.Equal(HttpStatusCode.OK, comment.StatusCode);

        using var photoContent = new MultipartFormDataContent();
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };
        photoContent.Add(new ByteArrayContent(png), "file", "site.png");
        var upload = await owner.PostAsync($"/api/projects/{projectId}/notes/{note.Id}/photos", photoContent);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var photo = (await upload.Content.ReadFromJsonAsync<ProjectNotePhotoDto>())!;
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(
            $"/api/projects/{projectId}/notes/{note.Id}/photos/{photo.Id}")).StatusCode);
        var download = await owner.GetAsync($"/api/projects/{projectId}/notes/{note.Id}/photos/{photo.Id}");
        Assert.Equal(png, await download.Content.ReadAsByteArrayAsync());
        var saved = (await owner.GetFromJsonAsync<ProjectNoteDto>($"/api/projects/{projectId}/notes/{note.Id}"))!;
        Assert.Single(saved.Photos);
        Assert.Single(saved.Comments);
    }
}
