using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using ItemGroups.Interfaces.IQuestionnaireRepository;
using Microsoft.Extensions.Configuration;

public class QuestionnaireRepository : IQuestionnaireRepository
{
    private readonly string _connectionString;

    public QuestionnaireRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Retrieves the coded values associated with a given GUID.
    /// </summary>
    /// <param name="guid">The GUID of the client document.</param>
    /// <returns>A list of coded values.</returns>
    public List<string> GetCodedValues(string guid)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
                SELECT 
                    scm.CodedValue 
                FROM 
                    SCMObsCodedValue scm
                JOIN 
                    CV3ObservationDocument obsdoc ON scm.ParentGUID = obsdoc.ObsMasterItemGUID
                JOIN  
                    cv3clientdocument clientdoc ON clientdoc.GUID = obsdoc.OwnerGUID
                WHERE  
                    clientdoc.GUID = @guid";

            var param = new DynamicParameters();
            param.Add("guid", guid);
            var result = connection.Query<string>(query, param).ToList();

            return result;
        }
    }
}
