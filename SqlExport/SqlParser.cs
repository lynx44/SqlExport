﻿using System;
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
            commandTextCopy = this.CleanComments(commandTextCopy);
            var declarations = this.FindDeclarations(ref commandTextCopy);
            var assignments = this.FindAssignments(ref commandTextCopy);

            var paramDeclarations = declarations.Select(d => this.ParseDeclaration(d)).ToList();
            paramDeclarations.Join(assignments, 
                    d => d.Name.ToLower(), 
                    a => this.GetParameterName(a).ToLower(), 
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
            var typeDeclaration = Regex.Match(declaration, "[A-z]+(\\(\\d+(,\\s*\\d+){0,1}\\)){0,1}\\s*?($|;)").Value.Trim();
            string typeName;
            if (typeDeclaration.Contains("("))
            {
                var lengthArgs = typeDeclaration.Substring(typeDeclaration.IndexOf("(") + 1, typeDeclaration.IndexOf(")") - typeDeclaration.IndexOf("(") - 1);
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

            SqlDbType sqlDbType;
            if (Enum.TryParse(typeName, true, out sqlDbType))
            {
                paramDec.DbType = sqlDbType;
            }
            else
            {
                paramDec.DbType = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().
                    FirstOrDefault(t => t.ToString().StartsWith(typeName));
            }

            return paramDec;
        }

        private string ParseDefaultValue(string assignment)
        {
            var value = assignment.Split('=').Last()
                .Replace(";", string.Empty)
                .Replace("N'", string.Empty)
                .Replace("'", string.Empty)
                .Trim();

            return value;
        }

        private string GetParameterName(string snippet)
        {
            return Regex.Match(snippet, "@[A-z0-9_]+").Groups.Cast<Group>().Select(g => g.Value.Trim()).First();
        }

        private string CleanComments(string commandText)
        {
            commandText = Regex.Replace(commandText, "--.*", string.Empty);
            return commandText;
        }
    }

    public class SqlParameterDeclaration
    {
        public string Name { get; set; }
        public SqlDbType DbType { get; set; }
        public object DefaultValue { get; set; }
        public int? Size { get; set; }
        public int? Precision { get; set; }
    }
}
