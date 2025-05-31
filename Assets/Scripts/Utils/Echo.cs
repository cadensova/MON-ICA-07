// using UnityEngine;
// using System.Diagnostics;
// using System.Text.RegularExpressions;
// using System.Collections.Generic;
// using System.IO;
// using System;


// public static class Echo
// {
//     public static bool useCustomViewer = true;

//     private static readonly HashSet<string> importantNames = new()
//     {
//         "Player", "Dummy", "ActorRoot", "Target", "Enemy"
//     };

//     private const string ImportantNameColor = "#ffcc00";
//     private const string ActionNameColor = "#ffaa55";

//     private static readonly Dictionary<string, string> callerColors = new();
//     private static readonly string[] colorPool = new[]
//     {
//         "#FFE0B2", // soft peach
//         "#E6B8AF", // dusty rose
//         "#C19A8B", // warm taupe
//         "#B19CD9", // lavender haze
//         "#A28FA0", // muted mauve
//         "#8E7CC3", // soft plum
//         "#6C5B7B", // eggplant
//         "#D3B6A5", // soft clay
//         "#BCAAA4", // soft ash
//         "#A1887F", // warm earth
//         "#D7CCC8", // foggy sand
//         "#9C8AA5", // misty lilac
//         "#B39DDB", // smoky violet
//         "#B48EAD", // dusty pink
//         "#7E6C88", // desaturated indigo
//     };



//     private static int colorIndex = 0;

//     public static void Log(string message, params object[] args) =>
//         Print(message, LogLevel.Info, args);

//     public static void Warn(string message, params object[] args) =>
//         Print(message, LogLevel.Warning, args);

//     public static void Error(string message, params object[] args) =>
//         Print(message, LogLevel.Error, args);

//     private enum LogLevel { Info, Warning, Error }

//     private static void Print(string template, LogLevel level, object[] args)
//     {
//         string caller = GetCallingType();
//         string callerColor = GetOrAssignColor(caller);
//         string coloredCaller = $"<color={callerColor}>[{caller}]</color>";

//         string formatted = args.Length > 0
//             ? FormatMessageWithHighlights(template, args)
//             : FormatMessageWithHighlights(template);

//         string final = $"{coloredCaller} {formatted}";

//         if (useCustomViewer)
//         {
// #if UNITY_EDITOR
//             // string displayName = TryGetGameObjectNameFromStack() ?? caller;
//             // OnEcho?.Invoke(caller, displayName, formatted);
//             string plainCaller = $"[{caller}]";
//             WriteToFile($"{plainCaller} {StripRichText(formatted)}");
// #endif
//         }
//         else
//         {
//             switch (level)
//             {
//                 case LogLevel.Info: UnityEngine.Debug.Log(final); break;
//                 case LogLevel.Warning: UnityEngine.Debug.LogWarning(final); break;
//                 case LogLevel.Error: UnityEngine.Debug.LogError(final); break;
//             }
//         }


//     }

//     private static string StripRichText(string input)
//     {
//         input = Regex.Replace(input, @"<.*?>", string.Empty);
//         input = Regex.Replace(input, @"\{#.*?\}", string.Empty);
//         return input;
//     }

// #if UNITY_EDITOR
//     private static string TryGetGameObjectNameFromStack()
//     {
//         var stack = new StackTrace();
//         for (int i = 2; i < stack.FrameCount; i++)
//         {
//             var method = stack.GetFrame(i).GetMethod();
//             if (method.DeclaringType == null || method.DeclaringType == typeof(Echo)) continue;

//             var declaringType = method.DeclaringType;
//             if (declaringType.FullName.Contains("<")) continue;

//             foreach (var obj in UnityEngine.Object.FindObjectsByType(declaringType, UnityEngine.FindObjectsSortMode.None))
//             {
//                 if (obj is Component c)
//                     return c.gameObject.name;
//             }
//         }
//         return null;
//     }
// #endif

//     private static string GetCallingType()
//     {
//         var stack = new StackTrace();
//         for (int i = 2; i < stack.FrameCount; i++)
//         {
//             var frame = stack.GetFrame(i);
//             var method = frame.GetMethod();
//             var declaring = method.DeclaringType;
//             if (declaring != typeof(Echo) && declaring != null)
//                 return declaring.Name;
//         }
//         return "Unknown";
//     }

//     private static readonly string settingsPath = Path.Combine(
//     System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
//     "Code", "User", "settings.json");
//     private static string GetOrAssignColor(string caller)
//     {
//         if (callerColors.TryGetValue(caller, out string existing))
//             return existing;

//         string reused = TryReadColorFromVSCodeSettings(caller);
//         if (!string.IsNullOrEmpty(reused))
//         {
//             callerColors[caller] = reused;
//             return reused;
//         }

//         // Assign a new one
//         string color = colorPool[colorIndex % colorPool.Length];
//         colorIndex++;
//         callerColors[caller] = color;

//         TryInjectIntoVSCodeSettings(caller, color);
//         return color;
//     }

//     private static string TryReadColorFromVSCodeSettings(string caller)
//     {
//         try
//         {
//             if (!File.Exists(settingsPath)) return null;

//             string json = File.ReadAllText(settingsPath);
//             string pattern = $"\\\\\\[{caller}\\\\\\].*?\"foreground\":\\s*\"(#[a-fA-F0-9]+)\"";

//             var match = Regex.Match(json, pattern);
//             if (match.Success)
//             {
//                 return match.Groups[1].Value;
//             }
//         }
//         catch (Exception e)
//         {
//             UnityEngine.Debug.LogWarning($"Echo: Failed to read color from settings.json — {e.Message}");
//         }

//         return null;
//     }


//     private static void TryInjectIntoVSCodeSettings(string caller, string color)
//     {
//         try
//         {
//             if (!File.Exists(settingsPath)) return;

//             string json = File.ReadAllText(settingsPath);
//             if (!json.Contains("logFileHighlighter.customPatterns"))
//             {
//                 UnityEngine.Debug.LogWarning("Echo: Could not find 'logFileHighlighter.customPatterns' in settings.json.");
//                 return;
//             }

//             string fullPattern = $"{{ \"pattern\": \"\\\\[{caller}\\\\]\", \"foreground\": \"{color}\", \"fontStyle\": \"bold\" }}";

//             if (json.Contains(fullPattern)) return; // exact rule already exists

//             // Don't insert if caller is already in *any* customPattern rule
//             if (Regex.IsMatch(json, $"\"pattern\"\\s*:\\s*\"\\\\\\\\\\[{Regex.Escape(caller)}\\\\\\\\\\]\""))
//             {
//                 UnityEngine.Debug.Log($"Echo: Pattern for [{caller}] already exists. Skipping injection.");
//                 return;
//             }



//             // Find insertion point
//             var patternListMatch = Regex.Match(json, @"""logFileHighlighter\.customPatterns""\s*:\s*\[\s*", RegexOptions.Multiline);
//             if (!patternListMatch.Success)
//             {
//                 UnityEngine.Debug.LogWarning("Echo: Could not locate 'logFileHighlighter.customPatterns' array.");
//                 return;
//             }

//             int insertIndex = patternListMatch.Index + patternListMatch.Length;

//             if (insertIndex < 0) return;

//             // Build the new pattern block
//             string newEntry = $"{{ \"pattern\": \"\\\\[{caller}\\\\]\", \"foreground\": \"{color}\", \"fontStyle\": \"bold\" }},\n";

//             // Insert it
//             json = json.Insert(insertIndex, newEntry);
//             File.WriteAllText(settingsPath, json);

//             UnityEngine.Debug.Log($"Echo: Injected color rule for [{caller}] into VS Code settings.");
//         }
//         catch (Exception e)
//         {
//             UnityEngine.Debug.LogWarning($"Echo: Failed to update VS Code settings.json — {e.Message}");
//         }
//     }


//     private static string FormatMessageWithHighlights(string template, object[] args)
//     {
//         int index = 0;
//         return Regex.Replace(template, @"{(.*?)}", match =>
//         {
//             if (index >= args.Length) return match.Value;
//             string value = args[index]?.ToString() ?? "null";
//             index++;
//             return $"<color=#00ffff><b>{value}</b></color>";
//         });
//     }

//     private static string FormatMessageWithHighlights(string message)
//     {
//         message = Regex.Replace(message, @"(\b\w+\b):\s*(\d+)", m =>
//         {
//             string label = m.Groups[1].Value;
//             string value = m.Groups[2].Value;
//             return $"{label}: <color=#00ffff><b>{value}</b></color>";
//         });

//         message = Regex.Replace(message, @"\b(true|false|null)\b", "<color=#00ffff><b>$0</b></color>", RegexOptions.IgnoreCase);
//         message = Regex.Replace(message, @"(?<![A-Za-z])\b\d+(\.\d+)?\b(?!\s*\)|[^>]*</color>)", "<color=#00ffff><b>$0</b></color>");

//         message = Regex.Replace(message, @"\b[A-Z][a-z]+(?:[A-Z][a-z]+)+\b", m =>
//         {
//             string word = m.Value;
//             return $"<color={ActionNameColor}><b>{word}</b></color>";
//         });

//         message = Regex.Replace(message, @"(on target: |on )(\w+(?: \(\d+\))?)", m =>
//         {
//             string prefix = m.Groups[1].Value;
//             string name = m.Groups[2].Value;
//             return $"{prefix}<color={ActionNameColor}><b>{name}</b></color>";
//         });

//         foreach (var name in importantNames)
//         {
//             message = Regex.Replace(message, $@"\b{name}(?: ?\(\d+\))?", m =>
//             {
//                 string match = m.Value;
//                 return $"<color={ImportantNameColor}><b>{match}</b></color>";
//             }, RegexOptions.IgnoreCase);
//         }

//         return message;
//     }

//     private static readonly string logFilePath = @"C:\Users\Caden Sova\Documents\GitHub\BestMovement\Assets\EchoDebugLog.log";
//     private static bool wroteSessionHeader = false;

//     private static void WriteToFile(string cleanLine)
//     {
//         string timestamp = $"[{Time.realtimeSinceStartup:F2}]";

//         if (!wroteSessionHeader)
//         {
//             cleanLine = $"--- Echo Log Session Start [{System.DateTime.Now}] ---\n" + cleanLine;
//             wroteSessionHeader = true;
//         }

//         string line = $"{timestamp} {cleanLine}";

//         System.Threading.Tasks.Task.Run(() =>
//         {
//             try
//             {
//                 System.IO.File.AppendAllText(logFilePath, line + "\n");
//             }
//             catch (System.Exception e)
//             {
//                 UnityEngine.Debug.LogWarning("Echo: Failed to write to log file — " + e.Message);
//             }
//         });
//     }
    
//     #if UNITY_EDITOR
//     [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
//     private static void ClearLogFileOnPlay()
//     {
//         try
//         {
//             if (File.Exists(logFilePath))
//             {
//                 File.WriteAllText(logFilePath, string.Empty);
//                 UnityEngine.Debug.Log("Echo: Cleared log file at play start.");
//             }
//         }
//         catch (Exception e)
//         {
//             UnityEngine.Debug.LogWarning("Echo: Failed to clear log file — " + e.Message);
//         }

//         wroteSessionHeader = false; // ensure a fresh header will be written
//     }
//     #endif


// }
