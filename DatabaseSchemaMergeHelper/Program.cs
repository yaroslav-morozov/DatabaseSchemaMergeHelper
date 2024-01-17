namespace DatabaseSchemaMergeHelper;

using Microsoft.SqlServer.Dac.Compare;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Iterates through a supplied schema compare file and excludes objects belonging to a supplied list of schema
/// </summary>
class Program
{
    /// <summary>
    /// first argument is the scmp file to update, second argument is comma separated list of schemas to exclude
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        string scmpFilePath = args[0];
        List<string> listOfSchemasToExclude = args[1].Split(',').ToList();

        // load comparison from Schema Compare (.scmp) file
        SchemaComparison comparison = new(scmpFilePath);
        SchemaComparisonResult comparisonResult = comparison.Compare();

        // find changes pertaining to objects belonging to the supplied schema exclusion list
        List<SchemaDifference> listOfDifferencesToExclude = new();

        // add those objects to a list
        foreach (SchemaDifference difference in comparisonResult.Differences)
        {
            if (difference.TargetObject != null &&
                difference.TargetObject.Name != null &&
                difference.TargetObject.Name.HasName &&
                listOfSchemasToExclude.Contains(difference.TargetObject.Name.Parts[0], StringComparer.OrdinalIgnoreCase))
            {
                listOfDifferencesToExclude.Add(difference);
            }
        }

        // add the needed exclusions to the .scmp file
        foreach (SchemaDifference diff in listOfDifferencesToExclude)
        {
            if (diff.SourceObject != null)
            {
                SchemaComparisonExcludedObjectId SourceExclusionObject = new(diff.SourceObject.ObjectType, diff.SourceObject.Name,
                                                                                 diff.Parent?.SourceObject.ObjectType, diff.Parent?.SourceObject.Name);
                comparison.ExcludedSourceObjects.Add(SourceExclusionObject);
            }

            SchemaComparisonExcludedObjectId TargetExclusionObject = new(diff.TargetObject.ObjectType, diff.TargetObject.Name,
                                                                             diff.Parent?.TargetObject.ObjectType, diff.Parent?.TargetObject.Name);
            comparison.ExcludedTargetObjects.Add(TargetExclusionObject);
        }

        // save the file, overwrites the existing scmp.
        comparison.SaveToFile(scmpFilePath, true);
    }
}
