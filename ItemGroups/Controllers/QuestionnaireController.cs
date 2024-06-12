using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ItemGroups.Interfaces.IQuestionnaireRepository;

namespace ParentItemsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionnaireController : ControllerBase
    {
        private readonly IQuestionnaireRepository _repository;

        public QuestionnaireController(IQuestionnaireRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Represents the format of a question in the questionnaire.
        /// </summary>
        public class QuersionFormat
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="QuersionFormat"/> class.
            /// </summary>
            public QuersionFormat()
            {
                Items = new List<QuersionFormat>();
            }

            /// <summary>
            /// Gets or sets the link ID of the question.
            /// </summary>
            public string LinkID { get; set; }

            /// <summary>
            /// Gets the generation of the question based on the number of slashes in the link ID.
            /// </summary>
            [JsonIgnore]
            public int Generation
            {
                get
                {
                    return string.IsNullOrEmpty(LinkID) ? 0 : LinkID.Count(c => c == '/') - 1;
                }
            }

            /// <summary>
            /// Gets the native ID of the question, which is the last part of the link ID.
            /// </summary>
            [JsonIgnore]
            public string NativeId
            {
                get
                {
                    if (!string.IsNullOrEmpty(LinkID))
                    {
                        string[] parts = LinkID.Split('/');
                        if (parts.Length > 0)
                        {
                            return parts[parts.Length - 1];
                        }
                    }
                    return string.Empty;
                }
            }

            /// <summary>
            /// Gets the immediate parent ID of the question, which is the second-to-last part of the link ID.
            /// </summary>
            [JsonIgnore]
            public string ImmediateParentId
            {
                get
                {
                    if (!string.IsNullOrEmpty(LinkID))
                    {
                        string[] parts = LinkID.Split('/');
                        if (parts.Length >= 2)
                        {
                            return parts[parts.Length - 2];
                        }
                    }
                    return string.Empty;
                }
            }

            /// <summary>
            /// Gets or sets the question text.
            /// </summary>
            public string Question { get; set; }

            /// <summary>
            /// Gets or sets the list of child questions.
            /// </summary>
            public List<QuersionFormat> Items { get; set; }
        }

        /// <summary>
        /// Retrieves the questionnaire based on the provided GUID.
        /// </summary>
        /// <param name="guid">The GUID of the DocumentGUID.</param>
        /// <returns>The questionnaire with parent-child relationship built.</returns>
        [HttpGet("{guid}")]
        public IActionResult Get(string guid)
        {
            var codedValues = _repository.GetCodedValues(guid);
            if (codedValues == null || !codedValues.Any())
            {
                return NotFound("Resource Not Found.");
            }

            var questions = BuildQuestionsList(codedValues);
            var structuredQuestions = BuildParentChildRelation_ByLoop(questions);
            var result = BuildResult(structuredQuestions);

            return Ok(result);
        }

        /// <summary>
        /// Builds the parent-child relationship for a list of questions using a loop.
        /// </summary>
        /// <param name="questions">The list of questions.</param>
        /// <returns>The list of questions with the parent-child relationship built.</returns>
        public List<QuersionFormat> BuildParentChildRelation_ByLoop(List<QuersionFormat> questions)
        {
            int maxGeneration = questions.Max(q => q.Generation);
            for (int generation = maxGeneration; generation > 0; generation--)
            {
                var currentGenerationQuestions = questions.Where(q => q.Generation == generation).ToList();

                foreach (var childQuestion in currentGenerationQuestions)
                {
                    var parentQuestion = questions.FirstOrDefault(q => q.NativeId == childQuestion.ImmediateParentId);

                    if (parentQuestion != null)
                    {
                        parentQuestion.Items.Add(childQuestion);
                    }
                }
            }
            return questions.Where(q => q.Generation == 1).ToList();
        }

        /// <summary>
        /// Builds a list of questions from coded values.
        /// </summary>
        /// <param name="codedValues">The list of coded values.</param>
        /// <returns>The list of questions.</returns>
        public List<QuersionFormat> BuildQuestionsList(List<string> codedValues)
        {
            var questions = new List<QuersionFormat>();
            foreach (var codedValue in codedValues)
            {
                var fullID = GetFullID(codedValue);
                var question = GetQuestion(codedValue);

                var questionFormat = new QuersionFormat
                {
                    LinkID = fullID,
                    Question = question
                };

                questions.Add(questionFormat);
            }
            return questions;
        }

        /// <summary>
        /// Builds a dictionary result with the parent-child relationship for a list of structured questions.
        /// </summary>
        /// <param name="structuredQuestions">The list of structured questions.</param>
        /// <returns>A dictionary result with the parent-child relationship built.</returns>
        public Dictionary<string, List<QuersionFormat>> BuildResult(List<QuersionFormat> structuredQuestions)
        {
            var result = new Dictionary<string, List<QuersionFormat>>();

            foreach (var question in structuredQuestions)
            {
                var rootId = question.LinkID.Split('/')[1]; // Extract the root ID
                if (!result.ContainsKey(rootId))
                {
                    result[rootId] = new List<QuersionFormat>();
                }
                result[rootId].Add(question);
            }

            return result;
        }

        /// <summary>
        /// Gets the full ID from the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The full ID.</returns>
        public string GetFullID(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            int index = input.IndexOf('^');
            if (index >= 0)
            {
                return input.Substring(0, index);
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the question from the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The question.</returns>
        public static string GetQuestion(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            int caretIndex = input.IndexOf('^');
            if (caretIndex >= 0)
            {
                return input.Substring(caretIndex + 1).Split('^')[0];
            }

            return string.Empty;
        }
    }
}
