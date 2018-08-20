using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace SqlExport
{
    public class SqlParser
    {
        public IEnumerable<SqlParameterDeclaration> ParseVariables(string commandText)
        {
            var commandTextCopy = new string(commandText.ToCharArray());
            var declarations = this.FindDeclarations(ref commandTextCopy);
            var assignments = this.FindAssignments(ref commandTextCopy);

            var paramDeclarations = declarations.Select(d => this.ParseDeclaration(d)).ToList();
            paramDeclarations.Join(assignments, 
                    d => d.Name, 
                    a => this.GetParameterName(a), 
                    (s, a) => new { Param = s, Assignment = a}).ToList()
                .ForEach(o =>
                {
                    var defaultValue = this.ParseDefaultValue(o.Assignment);
                    o.Param.DefaultValue = defaultValue;
                });

            return paramDeclarations;
        }

        private IEnumerable<string> FindDeclarations(ref string commandText)
        {
            var semicolonMatches = this.FindMatches(commandText, "DECLARE\\s+@.*?;");
            commandText = Regex.Replace(commandText, "DECLARE\\s+@.*?;", string.Empty);
            var newlineMatches = this.FindMatches(commandText, "DECLARE\\s+@.*$");
            commandText = Regex.Replace(commandText, "DECLARE\\s+@.*$", string.Empty, RegexOptions.Multiline);
            return semicolonMatches.Concat(newlineMatches).ToList();
        }

        private IEnumerable<string> FindAssignments(ref string commandText)
        {
            var semicolonMatches = this.FindMatches(commandText, "SET\\s+@.*?;");
            commandText = Regex.Replace(commandText, "SET\\s+@.*?;", string.Empty);
            var newlineMatches = this.FindMatches(commandText, "SET\\s+@.*$");
            commandText = Regex.Replace(commandText, "SET\\s+@.*$", string.Empty, RegexOptions.Multiline);

            return semicolonMatches.Concat(newlineMatches).ToList();
        }

        private IEnumerable<string> FindMatches(string commandText, string regex)
        {
            var matchCollection = Regex.Matches(commandText, regex, RegexOptions.Multiline);
            return matchCollection.Cast<Match>().SelectMany(m => m.Groups.Cast<Group>().Select(g => g.Value)).ToList();
        }

        private SqlParameterDeclaration ParseDeclaration(string declaration)
        {
            var paramDec = new SqlParameterDeclaration();
            var parameterName = this.GetParameterName(declaration);
            paramDec.Name = parameterName;
            var typeDeclaration = declaration.Split(' ').Last();
            string typeName;
            if (typeDeclaration.Contains("("))
            {
                var lengthArgs = typeDeclaration.Substring(typeDeclaration.IndexOf("("), typeDeclaration.IndexOf(")") - typeDeclaration.IndexOf("("));
                var sizes = lengthArgs.Split(',');
                paramDec.Size = int.Parse(sizes.First());
                if (sizes.Length > 1)
                {
                    paramDec.Precision = int.Parse(sizes.Last());
                }

                typeName = typeDeclaration.Substring(0, typeDeclaration.IndexOf("("));
            }
            else
            {
                typeName = typeDeclaration.Replace(";", string.Empty);
            }

            paramDec.DbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), typeName, true);

            return paramDec;
        }

        private string ParseDefaultValue(string assignment)
        {
            var value = assignment.Split('=').Last()
                .Replace(";", string.Empty)
                .Replace("'", string.Empty)
                .Trim();

            return value;
        }

        private string GetParameterName(string snippet)
        {
            return Regex.Match(snippet, "@.+?\\s").Groups.Cast<Group>().Select(g => g.Value.Trim()).First();
        }
    }

    public class SqlParameterDeclaration
    {
        public string Name { get; set; }
        public SqlDbType DbType { get; set; }
        public object DefaultValue { get; set; }
        public int Size { get; set; }
        public int Precision { get; set; }
    }
}
