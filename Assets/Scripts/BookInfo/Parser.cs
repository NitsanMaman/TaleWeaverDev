// Filename: Parser.cs
// Author: Nitsan Maman & Ron Shahar
// Description: The Parser class is responsible for extracting encounter information from narrative text and formatting it into 
// a Page object for interactive storytelling. This class handles various mechanics like rolls, riddles, checks, combat, and luck scenarios.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Windows;
using TaleWeaver.Gameplay;

/// <summary>
/// Parses narrative text into structured game objects such as Page, Options, and encounters, handling different mechanics like roll checks, riddles, combat, and luck.
/// </summary>
public class Parser : MonoBehaviour
{
    public static Parser Instance { get; private set; }

    // Define custom strings for each position in roll outcomes
    Dictionary<int, string> rollOutcomes = new Dictionary<int, string>
        {
            {0, "(-2 life)"},
            {1, "(-1 life)"},
            {2, "(Nothing)"},
            {3, "(+1 luck)"},
            {4, "(+1 life)"}
        };

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // This means this instance was the singleton and is now being destroyed
            Debug.Log("Parser instance is being destroyed.");
            Instance = null;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Parses the narrative text and image path to create a Page object, which represents an encounter with mechanics and options.
    /// </summary>
    /// <param name="narrative">The narrative text describing the encounter.</param>
    /// <param name="imagePath">The path to the image related to the encounter.</param>
    /// <returns>A Page object with extracted encounter details, options, and mechanics.</returns>
    public Page ParsePage(string narrative, string imagePath)
    {
        try
        {
            // Split the narrative text into lines
            string[] lines = narrative.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Extract Encounter Number and Name
            string encounterLine = lines.FirstOrDefault(line => line.StartsWith("###"));
            if (encounterLine == null) throw new Exception("Encounter line not found");
            string[] encounterParts = encounterLine.Replace("###", "").Trim().Split(new[] { ':' }, 2);
            string encounterNum = encounterParts[0].Trim();
            string encounterName = encounterParts.Length > 1 ? encounterParts[1].Trim() : "";

            // Initialize sections
            string encounterIntroduction = "";
            string encounterDescription = "";
            string imageGeneration = "";
            string encounterMechanic = "";
            string encounterMechanicInfo = "";
            List<Option> choices = new List<Option>();

            // Split the narrative into parts based on the asterisks (**)
            string[] parts = narrative.Split(new[] { "**" }, StringSplitOptions.None);

            if (parts.Length >= 8)
            {
                encounterIntroduction = parts[2].Trim();
                encounterDescription = parts[4].Trim();
                imageGeneration = parts[6].Trim();
                encounterMechanic = parts[8].Trim();

                // Extract choices or other mechanics
                string[] choiceLines = encounterMechanic.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                encounterMechanic = choiceLines[0].Trim();

                if (encounterMechanic.StartsWith("$$Roll$$"))
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        var rollDescription = choiceLines.FirstOrDefault(line => line.StartsWith(i + "."));
                        if (rollDescription != null)
                        {
                            string[] rollParts = rollDescription.Substring(2).Trim().Split(new[] { "$$" }, StringSplitOptions.None);
                            choices.Add(new Option(rollParts[0].Trim(), GameMechanicsManager.Instance.rollResults[i - 1]));
                        }
                    }
                }
                else if (encounterMechanic.StartsWith("&&Riddle&&"))
                {
                    int RiddleInfoIndex = Array.FindIndex(choiceLines, line => line.StartsWith("&&RiddleDescription&&"));
                    if (RiddleInfoIndex != -1)
                    {
                        encounterMechanicInfo = choiceLines[RiddleInfoIndex + 1].Trim();
                    }

                    for (int i = 1; i <= 3; i++)
                    {
                        var riddleDescription = choiceLines.FirstOrDefault(line => line.StartsWith(i + "."));
                        if (riddleDescription != null)
                        {
                            choices.Add(new Option(riddleDescription.Substring(2).Trim(), "Wrong"));
                        }
                    }
                    // Correct answer
                    int correctAnswerIndex = Array.FindIndex(choiceLines, line => line.StartsWith("&&RiddleAns&&"));
                    if (correctAnswerIndex != -1)
                    {
                        var correctAnswer = choiceLines[correctAnswerIndex + 1].Trim();
                        string[] AnswerParts = correctAnswer.Split(new[] { '.' }, 2);
                        int correctindx = int.Parse(AnswerParts[0]);
                        choices[correctindx - 1].outcome = "Correct";
                    }
                }
                else if (encounterMechanic.StartsWith("%%Check%%"))
                {
                    int RiddleInfoIndex = Array.FindIndex(choiceLines, line => line.StartsWith("%%CheckDescription%%"));
                    if (RiddleInfoIndex != -1)
                    {
                        string checkDescription = choiceLines[RiddleInfoIndex + 1].Trim();
                        string checkNumber = choiceLines[RiddleInfoIndex + 2].Trim();
                        checkNumber = checkNumber.Replace("%", "");
                        choices.Add(new Option(checkDescription, checkNumber));
                    }
                }
                else if (encounterMechanic.StartsWith("##Combat##"))
                {
                    string monsterName = "";
                    int difficulty = 0;
                    foreach (var line in choiceLines)
                    {
                        if (line.StartsWith("##Name##"))
                        {
                            monsterName = line.Replace("##Name##", "").Trim();
                        }
                        else if (line.StartsWith("##Diff##"))
                        {
                            difficulty = int.Parse(line.Replace("##Diff##", "").Trim());
                        }
                    }
                    choices.Add(new Option($"Combat with {monsterName}", difficulty.ToString()));
                }
                else if (encounterMechanic.StartsWith("@@luck@@"))
                {
                    string scenario1 = "";
                    string scenario2 = "";
                    string scenario1Effect = "";
                    string scenario2Effect = "";
                    int i = 0;
                    foreach (var line in choiceLines)
                    {
                        if (line.StartsWith("@@scenario 1:@@"))
                        {
                            //string[] miniparts = line.Replace("@@senario 1:@@", "").Trim().Split(new[] { "@@", "@@luck1Description@@" }, StringSplitOptions.None);
                            //scenario1Effect = miniparts[0].Trim();
                            scenario1Effect = line.Replace("@@scenario 1:@@", "").Trim();
                            scenario1Effect = scenario1Effect.Replace("@@", "").Trim();
                            //scenario1 = miniparts.Length > 1 ? miniparts[1].Trim() : "";
                        }
                        if (line.StartsWith("@@luck1Description@@"))
                        {
                            scenario1 = choiceLines[i + 1];
                        }
                        else if (line.StartsWith("@@scenario 2:@@"))
                        {
                            //string[] miniparts = line.Replace("@@senario 2:@@", "").Trim().Split(new[] { "@@", "@@luck2Description@@" }, StringSplitOptions.None);
                            //scenario2Effect = miniparts[0].Trim();
                            scenario2Effect = line.Replace("@@scenario 2:@@", "").Trim();
                            scenario2Effect = scenario2Effect.Replace("@@", "").Trim();
                            //scenario2 = miniparts.Length > 1 ? miniparts[1].Trim() : "";
                        }
                        if (line.StartsWith("@@luck2Description@@"))
                        {
                            scenario2 = choiceLines[i + 1];
                        }
                        i++;
                    }
                    choices.Add(new Option(scenario1, scenario1Effect));
                    choices.Add(new Option(scenario2, scenario2Effect));
                }
                else
                {
                    bool firstOptionsLine = true;
                    string[] opANDef;
                    string option = "";
                    string effect = "";
                    foreach (var line in choiceLines)
                    {
                        if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3."))
                        {
                            opANDef = line.Split(new[] { "!!" }, StringSplitOptions.None);
                            option = opANDef[0];
                            effect = opANDef[1];
                            choices.Add(new Option(option, effect));
                        }
                        else
                        {
                            if (firstOptionsLine && (!line.StartsWith("!!")))
                            {
                                firstOptionsLine = false;
                                encounterMechanicInfo = line;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Narrative format is incorrect.");
            }

            // Truncate sections to their word limits
            encounterIntroduction = TruncateText(encounterIntroduction, 145);
            encounterDescription = TruncateText(encounterDescription, 600);
            List<int> encounterStats = new List<int> { 10, 2, 0 };

            //remove () if there are any
            // Regular expression to match parentheses and their content
            string pattern = @"\([^\)]*\)";
            // Replace the matched patterns with an empty string
            encounterNum = Regex.Replace(encounterNum, pattern, "").Trim();
            encounterName = Regex.Replace(encounterName, pattern, "").Trim();
            encounterIntroduction = Regex.Replace(encounterIntroduction, pattern, "").Trim();
            encounterDescription = Regex.Replace(encounterDescription, pattern, "").Trim();
            encounterMechanicInfo = Regex.Replace(encounterMechanicInfo, pattern, "").Trim();
            // Applying regex replace on each Option object's Text property
            for (int i = 0; i < choices.Count; i++)
            {
                if (encounterMechanic.StartsWith("$$Roll$$"))
                {
                    if (i == 5)
                        choices[i].option = RemoveSecondParenthesis(choices[i].option);
                    else
                        // Apply the replacement with the custom string
                        choices[i].option = Regex.Replace(choices[i].option, pattern, rollOutcomes[i]).Trim();
                }
                else
                {
                    choices[i].option = Regex.Replace(choices[i].option, pattern, "").Trim();
                }

                Console.WriteLine(choices[i].option);
            }
            /*            foreach (var choice in choices)
                        {
                            choice.option = RemoveSecondParenthesis(choice.option);
                            Console.WriteLine(choice.option);
                        }*/

            if (PlayerInGame.Instance != null)
            {
                encounterStats = new List<int> { PlayerInGame.Instance.currentHealth, PlayerInGame.Instance.currentLuck, PlayerInGame.Instance.currentSkillModifier };
            }
            return new Page(encounterNum, encounterName, encounterIntroduction, imageGeneration, encounterDescription, encounterMechanic, choices, imagePath, encounterStats, encounterMechanicInfo);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing page: " + ex.Message);
            ErrorHandler.Instance.ErrorAccured("Error parsing page: " + ex.Message);
            return null;
        }

    }

    private static string RemoveSecondParenthesis(string text)
    {
        // Find all matches of text in parentheses
        MatchCollection matches = Regex.Matches(text, @"\([^()]*\)");

        // Check if there are at least two parentheses
        if (matches.Count >= 2)
        {
            // Remove only the second match
            string toRemove = matches[1].Value;
            int index = text.IndexOf(toRemove);
            text = text.Remove(index, toRemove.Length);
        }

        return text;
    }

    /// <summary>
    /// Parses the narrative text for the Conclusion page of the game, creating a Page object representing the conclusion.
    /// </summary>
    /// <param name="messageContent">The content of the conclusion message.</param>
    /// <param name="imagePath">The image path for the conclusion.</param>
    /// <returns>A Page object with the conclusion details.</returns>
    public Page ParseConclusion(string messageContent, string imagePath)
    {
        // Split the conclusion content into lines
        string[] lines = messageContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Extract the encounter name and introduction
        string encounterName = "";
        string encounterIntroduction = "";
        bool introductionStarted = false;

        foreach (string line in lines)
        {
            if (line.StartsWith("### Conclusion:"))
            {
                encounterName = line.Replace("### Conclusion:", "").Trim();
            }
            else if (line.StartsWith("^^conclusion image description^^"))
            {
                break;
            }
            else
            {
                if (!introductionStarted)
                {
                    introductionStarted = true;
                    encounterIntroduction += line;
                }
                else
                {
                    encounterIntroduction += " " + line;
                }
            }
        }

        encounterIntroduction = encounterIntroduction.Replace("^^conclusion^^", "").Trim();

        //remove () if there are any
        // Regular expression to match parentheses and their content
        string pattern = @"\([^\)]*\)";
        // Replace the matched patterns with an empty string
        encounterName = Regex.Replace(encounterName, pattern, "").Trim();
        encounterIntroduction = Regex.Replace(encounterIntroduction, pattern, "").Trim();

        return new Page(
            encounterNum: "Conclusion",
            encounterName: encounterName,
            encounterIntroduction: encounterIntroduction,
            imageGeneration: "",
            encounterDetails: "",
            encounterMechanic: "",
            encounterMechanicInfo: "",
            encounterOptions: new List<Option>(),
            imageUrl: imagePath,
            encounterStats: new List<int> { PlayerInGame.Instance.currentHealth, PlayerInGame.Instance.currentLuck, PlayerInGame.Instance.currentSkillModifier }
        );
    }


    private string TruncateText(string text, int maxWords)
    {
        string[] words = text.Split(' ');
        if (words.Length > maxWords)
        {
            return string.Join(" ", words, 0, maxWords) + "...";
        }
        return text;
    }

    /// <summary>
    /// Extracts the image description section from the provided message content.
    /// </summary>
    /// <param name="messageContent">The content of the message.</param>
    /// <param name="isconc">Indicates if this is for a conclusion encounter.</param>
    /// <returns>The extracted image description text.</returns>
    public string ExtractImageDescription(string messageContent, bool isconc)
    {
        if (string.IsNullOrEmpty(messageContent))
            return null;

        if (isconc)
        {
            string conclusionStartTag = "^^conclusion^^";
            string imageDescriptionTag = "^^conclusion image description^^";

            int conclusionStartIndex = messageContent.IndexOf(conclusionStartTag, StringComparison.OrdinalIgnoreCase);
            int imageDescriptionStartIndex = messageContent.IndexOf(imageDescriptionTag, StringComparison.OrdinalIgnoreCase);

            if (conclusionStartIndex != -1 && imageDescriptionStartIndex != -1)
            {
                string imageGeneration = messageContent.Substring(imageDescriptionStartIndex + imageDescriptionTag.Length).Trim();
                return imageGeneration;
            }
            else
            {
                Debug.LogWarning("Conclusion image description section not found in message content.");
                return null;
            }
        }
        else
        {
            string[] parts = messageContent.Split(new[] { "**" }, StringSplitOptions.None);
            if (parts.Length >= 6)
            {
                string imageGeneration = parts[6].Trim();

                // Optionally, you could split by a known section starter if you want to cut off at that point
                string[] possibleEndTags = new[] { "Encounter Description:", "Mechanics:" };
                foreach (var endTag in possibleEndTags)
                {
                    int endIndex = imageGeneration.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);
                    if (endIndex != -1)
                    {
                        imageGeneration = imageGeneration.Substring(0, endIndex).Trim();
                        break;
                    }
                }

                return imageGeneration;
            }
            else
            {
                Debug.LogWarning("Image generation section not found in message content.");
                return null;
            }
        }
    }

}