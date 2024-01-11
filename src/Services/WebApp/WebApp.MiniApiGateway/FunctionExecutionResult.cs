﻿namespace WebApp.MiniApiGateway;

public class FunctionExecutionResult
{
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public string? AdditionalLogMessage { get; set; }
    public TimeSpan Duration { get; set; }
}