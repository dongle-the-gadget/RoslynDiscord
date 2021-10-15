using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoslynDiscord
{
    public class CompileCommands : BaseCommandModule
    {
        private static Regex regex = new Regex("```[\\s\\S]*```");

        [Command("compile")]
        [RequireOwner]
        public async Task CompileAsync(CommandContext ctx, [RemainingText] string code)
        {
            await ctx.TriggerTypingAsync();
            Stopwatch stopwatch = Stopwatch.StartNew();
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            string match = regex.Match(code).Value.Replace("```", "");
            using (MemoryStream ms = new MemoryStream())
            {
                SourceText codeString = SourceText.From(match);
                CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);
                SyntaxTree parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
                List<MetadataReference> references = new List<MetadataReference>()
                {
                    MetadataReference.CreateFromFile(typeof (AssemblyTargetedPatchBandAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof (CSharpArgumentInfo).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof (CompileContext).Assembly.Location),
                    MetadataReference.CreateFromFile((AppDomain.CurrentDomain.GetAssemblies()).Single(a => a.GetName().Name == "netstandard").Location)
                };
                Assembly.GetEntryAssembly().GetReferencedAssemblies().ToList().ForEach(f => references.Add(MetadataReference.CreateFromFile(Assembly.Load(f).Location)));
                CSharpCompilation compilation = CSharpCompilation.Create("Hello.dll", new SyntaxTree[1]
                {
                    parsedSyntaxTree
                }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
                EmitResult result = compilation.Emit(ms);
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                if (failures.Count() > 0)
                {
                    stopwatch.Stop();
                    embed.WithColor(DiscordColor.IndianRed);
                    embed.WithTitle("Compiling failed");
                    embed.AddField("Errors", "```" + string.Join("\n", failures.Select(f => string.Format("{0}: {1}",f.Id, f.GetMessage()))) + "```");
                    DiscordMessage discordMessage = await ctx.RespondAsync(embed.Build());
                    return;
                }
                ms.Seek(0L, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                long compileTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
                stopwatch.Start();
                CompileContext context = new CompileContext(ctx.Client, ctx.Message, ctx.Member);
                try
                {
                    await (Task)assembly.GetType("Program").GetMethod("MainAsync").Invoke(null, new CompileContext[1]
                    {
                        context
                    });
                    long runningTime = stopwatch.ElapsedMilliseconds;
                    stopwatch.Stop();
                    embed.WithColor(DiscordColor.SpringGreen);
                    embed.WithTitle("Code execution succeeded");
                    embed.AddField("Code output", string.IsNullOrEmpty(context.Output.ToString()) ? "None" : string.Format("```{0}```", context.Output));
                    embed.AddField("Compile time", string.Format("{0} ms", compileTime), true);
                    embed.AddField("Execution time", string.Format("{0} ms", runningTime), true);
                    DiscordMessage discordMessage = await ctx.RespondAsync(embed.Build());
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    embed.WithColor(DiscordColor.IndianRed);
                    embed.WithTitle("Runtime exception");
                    embed.AddField("Exception message", ex is TargetInvocationException ? ex.InnerException.Message : ex.Message);
                    embed.AddField("Code output", string.IsNullOrEmpty(context.Output.ToString()) ? "None" : string.Format("```{0}```", context.Output));
                    DiscordMessage discordMessage = await ctx.RespondAsync(embed.Build());
                }
            }
        }
    }
}
