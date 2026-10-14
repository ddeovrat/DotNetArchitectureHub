using System.Text;
using System.Text.Json;

// ====================================================================
// 1. CONFIGURATION & API KEY VALIDATION
// ====================================================================
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (string.IsNullOrEmpty(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ ERROR: GEMINI_API_KEY environment variable is not set.");
    Console.ResetColor();
    return;
}

using var httpClient = new HttpClient();
var userQuestion = "What is the exact refund policy for cancellations made within 24 hours at DotnetArchitectureHub?";

Console.Clear();

// ====================================================================
// TIER 1: ADVANCED PROMPT ENGINEERING (PERSONA + SYSTEM MSG + FEW-SHOT)
// ====================================================================
Console.WriteLine("==================================================");
Console.WriteLine(" 1️⃣ TIER 1: ADVANCED PROMPT ENGINEERING ONLY");
Console.WriteLine("    (Includes Persona, System Instructions & Few-Shot Examples)");
Console.WriteLine("==================================================");

var promptEngineeringOnly = $"""
    [SYSTEM PERSONA]: You are a Senior Customer Support Specialist at DotnetArchitectureHub. Your tone is polite, formal, and authoritative.

    [FEW-SHOT EXAMPLES]:
    User: How do I change my billing email?
    Assistant: Greeting! To update your billing email at DotnetArchitectureHub, navigate to Account Settings > Billing > Email Preference.

    User: What payment methods do you accept?
    Assistant: Greetings! DotnetArchitectureHub accepts major credit cards (Visa, MasterCard), PayPal, and enterprise Wire Transfers.

    [USER QUESTION]:
    {userQuestion}
    """;

Console.WriteLine("Querying AI with Prompt Engineering (No Internal Knowledge)...");
var tier1Response = await CallGeminiApiAsync(httpClient, apiKey, promptEngineeringOnly);

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\n[Tier 1 Output (Prompting Only)]:");
Console.WriteLine(tier1Response);
Console.ResetColor();

Console.WriteLine("\n⚠️ TAKEAWAY: Prompt engineering enforced the formal tone and structure, BUT the AI guessed/hallucinated the policy because it lacks private company data.");
Console.WriteLine("\nPress ENTER to execute Tier 2 (RAG)...");
Console.ReadLine();

Console.Clear();

// ====================================================================
// TIER 2: PROMPT ENGINEERING + RAG (RETRIEVAL-AUGMENTED GENERATION)
// ====================================================================
Console.WriteLine("==================================================");
Console.WriteLine(" 2️⃣ TIER 2: PROMPT ENGINEERING + RAG");
Console.WriteLine("    (Injecting Dynamic Enterprise Data at Runtime)");
Console.WriteLine("==================================================");

Console.WriteLine("1. Retrieving internal document chunk...");
var knowledgeBase = GetInternalKnowledgeBase();
var retrievedContext = RetrieveRelevantContext(userQuestion, knowledgeBase);

Console.ForegroundColor = ConsoleColor.DarkCyan;
Console.WriteLine($"   [Retrieved Document]: {retrievedContext}");
Console.ResetColor();

var promptWithRag = $"""
    [SYSTEM PERSONA]: You are a Senior Customer Support Specialist at DotnetArchitectureHub. Your tone is polite, formal, and authoritative.

    [GROUNDING CONTEXT]:
    Use ONLY the factual information below to answer the question.
    {retrievedContext}

    [FEW-SHOT EXAMPLES]:
    User: How do I change my billing email?
    Assistant: Greetings! To update your billing email at DotnetArchitectureHub, navigate to Account Settings > Billing > Email Preference.

    [USER QUESTION]:
    {userQuestion}
    """;

Console.WriteLine("\n2. Querying AI with Grounded RAG Context...");
var tier2Response = await CallGeminiApiAsync(httpClient, apiKey, promptWithRag);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n[Tier 2 Output (RAG Grounded)]:");
Console.WriteLine(tier2Response);
Console.ResetColor();

Console.WriteLine("\n🟢 TAKEAWAY: RAG solved the knowledge gap! The AI returned the exact 2026 refund policy (100% full refund) without hallucinating.");
Console.WriteLine("\nPress ENTER to execute Tier 3 (Fine-Tuning Scenario)...");
Console.ReadLine();

Console.Clear();

// ====================================================================
// TIER 3: WHEN FINE-TUNING IS REQUIRED (BEHAVIOR & STRICT ENFORCEMENT)
// ====================================================================
Console.WriteLine("==================================================");
Console.WriteLine(" 3️⃣ TIER 3: FINE-TUNING SCENARIO");
Console.WriteLine("    (When Prompting + RAG output formatting fails or costs too many tokens)");
Console.WriteLine("==================================================");

Console.WriteLine("PROBLEM: Suppose our C# core system requires a strict, custom binary wire format or hyper-specialized C# domain DSL payload.");
Console.WriteLine("Passing huge system prompts, personas, and 10 few-shot examples on EVERY API call wastes thousands of tokens and occasionally fails schema parsing.\n");

// Simulating Fine-Tuned System behavior
var fineTunedPrompt = $"""
    [FINE-TUNED MODEL: dotnet-saas-support-v1]
    Context: {retrievedContext}
    Question: {userQuestion}
    """;

Console.WriteLine("Sending concise prompt to Fine-Tuned Model Endpoint...");
var tier3Response = await CallGeminiApiAsync(httpClient, apiKey, fineTunedPrompt);

// Sanitize string output for JSON embedding
var sanitizedResponse = tier3Response.Replace("\"", "'").Replace("\r", "").Replace("\n", " ");

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n[Tier 3 Simulated Fine-Tuned Output]:");

// FIX: Use $$""" raw string literal where single '{' is literal JSON, and '{{...}}' is C# interpolation
Console.WriteLine($$"""
    {
      "ticket_type": "REFUND_REQUEST",
      "policy_id": "DOC-101",
      "eligible": true,
      "refund_percentage": 100,
      "processing_fee": 0,
      "response_text": "{{sanitizedResponse}}"
    }
    """);
Console.ResetColor();

Console.WriteLine("\n==================================================");
Console.WriteLine(" 💡 ARCHITECTURAL DECISION MATRIX:");
Console.WriteLine(" 1️⃣ Prompt Engineering : Controls tone, style & persona (Zero infra cost).");
Console.WriteLine(" 2️⃣ RAG                : Supplies enterprise facts/data at runtime (Medium complexity).");
Console.WriteLine(" 3️⃣ Fine-Tuning        : Modifies base model weights for specialized behavior/schemas (Highest cost).");
Console.WriteLine("==================================================");


// ====================================================================
// INTERNAL RAG LOGIC & API HELPER METHODS
// ====================================================================

static List<DocumentChunk> GetInternalKnowledgeBase()
{
    return new List<DocumentChunk>
    {
        new("DOC-101", "Cancellation Policy",
            "DotnetArchitectureHub Refund Policy v2026: Cancellations requested within 24 hours of purchase receive a 100% full refund with zero processing fees. Processed within 3-5 business days."),
        new("DOC-102", "Late Cancellation Policy",
            "Cancellations made after 24 hours are non-refundable but receive a 50% store credit applicable to future billing cycles."),
        new("DOC-103", "Security Policy",
            "DotnetArchitectureHub encrypts all customer data at rest using AES-256 and requires multi-factor authentication for administrative access.")
    };
}

static string RetrieveRelevantContext(string query, List<DocumentChunk> docs)
{
    var queryKeywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var bestMatch = docs
        .Select(doc => new
        {
            Doc = doc,
            Score = queryKeywords.Count(kw => doc.Content.Contains(kw, StringComparison.OrdinalIgnoreCase))
        })
        .OrderByDescending(x => x.Score)
        .FirstOrDefault()?.Doc;

    return bestMatch != null
        ? $"[{bestMatch.Id} - {bestMatch.Title}]: {bestMatch.Content}"
        : "No relevant internal documentation found.";
}

static async Task<string> CallGeminiApiAsync(HttpClient client, string apiKey, string prompt)
{
    string[] candidateModels = ["gemini-3.8-flash", "gemini-3.6-flash", "gemini-3.5-flash-lite", "gemini-2.5-flash"];
    List<string> errors = new();

    foreach (var model in candidateModels)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var response = await client.PostAsync(endpoint, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    return doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "No output generated.";
                }

                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt == 1)
                {
                    await Task.Delay(1500);
                    continue;
                }

                errors.Add($"[{model}] ({response.StatusCode}): {responseJson}");
                break;
            }
            catch (Exception ex)
            {
                errors.Add($"[{model}] Exception: {ex.Message}");
                break;
            }
        }
    }

    return "API Connection Failed across candidate models:\n" + string.Join("\n", errors);
}

record DocumentChunk(string Id, string Title, string Content);