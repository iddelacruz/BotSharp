using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Knowledges.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;

namespace BotSharp.Core.Knowledges;

[Authorize]
[ApiController]
public class KnowledgeController : ControllerBase, IApiAdapter
{
    private readonly IKnowledgeService _knowledgeService;
    public KnowledgeController(IKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [HttpGet("/knowledge")]
    public async Task<string> GetAnswer([FromQuery(Name = "q")] string question)
    {
        return await _knowledgeService.GetAnswer(question);
    }

    [HttpPost("/knowledge/feed/{agentId}")]
    public async Task<IActionResult> FeedKnowledge([FromRoute] string agentId, List<IFormFile> files)
    {
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            if (formFile.Length <= 0)
            {
                continue;
            }

            var filePath = Path.GetTempFileName();

            using (var stream = System.IO.File.Create(filePath))
            {
                await formFile.CopyToAsync(stream);
            }

            var document = PdfDocument.Open(filePath);
            var content = "";
            foreach (Page page in document.GetPages())
            {
                content += page.Text;
            }

            // Process uploaded files
            // Don't rely on or trust the FileName property without validation.

            await _knowledgeService.Feed(new KnowledgeFeedModel
            {
                AgentId = agentId,
                Content = content
            });
        }

        return Ok(new { count = files.Count, size });
    }
}