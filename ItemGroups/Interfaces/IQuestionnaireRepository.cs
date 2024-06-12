namespace ItemGroups.Interfaces.IQuestionnaireRepository
{
    /// <summary>
    /// Represents a repository for managing questionnaires.
    /// </summary>
    public interface IQuestionnaireRepository
    {
        /// <summary>
        /// Gets the coded values for a given GUID.
        /// </summary>
        /// <param name="guid">The GUID of DocumentGUID.</param>
        /// <returns>A list of coded values.</returns>
        List<string> GetCodedValues(string guid);
    }

}
