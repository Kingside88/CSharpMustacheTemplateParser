using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace TemplateParserTest
{
    [TestClass]
    public class UnitTest1
    {
        public string Template1 => @"This is plain text bla bla bla
            {{for group in Groups}}
            The name of the grop is: {{group.Title}}

            {{for subGroup in group.SubGroups}}
            The name of the subGroup is: {{subGroup.Title}}
            Here is the name of the parent group: {{group.Title}}
            {{/for}}

            {{/for}}
            Some text in the end";

        [TestMethod]
        public void TestMethod1()
        {
            var shards = this.Template1.Split(Token.TemplateBegin, System.StringSplitOptions.RemoveEmptyEntries);

            List<INode> tree = new List<INode>();

            for (int i = 0; i < shards.Length; i++)
            {
                string shard = shards[i];

                ConditionalParseShard(shard, tree, shards, ref i);

            }
        }

        public void ConditionalParseShard(string shard, List<INode> tree, string[] shards, ref int i)
        {
            // Mit NodeFor beginnt das Template
            if (NodeForStatement.HasForStatement(shard))
            {
                var nodeForResult = NodeForStatement.ParseShard(shard);
                NodeForStatement nodeFor = nodeForResult.Item1;
                string rest = nodeForResult.Item2;

                ConditionalParseShard(rest, nodeFor.Children, shards, ref i);

                shard = shards[++i];
                while (i < shards.Length && !shard.Contains(NodeForStatement.TokenForEnd))
                {
                    ConditionalParseShard(shard, nodeFor.Children, shards, ref i);
                    shard = shards[++i];
                }

                shard = shard.Replace(NodeForStatement.TokenForEnd, string.Empty);

                tree.Add(nodeFor);

                ConditionalParseShard(shard, tree, shards, ref i);                
            }
            else if (NodeVariable.HasVariable(shard))
            {
                var nodeVariableResult = NodeVariable.ParseShard(shard);
                NodeVariable nodeVariable = nodeVariableResult.Item1;
                string rest = nodeVariableResult.Item2;

                tree.Add(nodeVariable);

                ConditionalParseShard(rest, tree, shards, ref i);
            }
            else
            {
                tree.Add(new NodePlainText() { Text = shard });                
            }            
        }
    }

    public static class Token
    {
        public const string TemplateBegin = "{{";
        public const string CommandEnd = "}}";
    }

    public interface INode
    {
        
    }

    public class NodePlainText : INode
    {
        public string Text { get; set; }        
    }

    public class NodeForStatement : INode
    {
        public string VariableName { get; set; }
        public string ListName { get; set; }
        public List<INode> Children { get; set; } = new List<INode>();
        public const string TokenForStart = "for";
        public const string TokenForEnd = "/for}}";
        public static bool HasForStatement(string shard)
        {
            return shard.Contains(TokenForStart) && shard.Contains(Token.CommandEnd);
        }

        public static (NodeForStatement, string) ParseShard(string shard)
        {
            NodeForStatement node = new NodeForStatement();

            var splitIn = shard.Split("in", System.StringSplitOptions.RemoveEmptyEntries);

            node.VariableName = splitIn[0].Replace("for", string.Empty).Trim();

            var indexAfterCommandEnd = splitIn[1].IndexOf(Token.CommandEnd);
            var commandLength = Token.CommandEnd.Length;

            node.ListName = splitIn[1].Substring(0, indexAfterCommandEnd).TrimStart();

            var rest = splitIn[1].Substring(indexAfterCommandEnd + commandLength, splitIn[1].Length - indexAfterCommandEnd - commandLength);

            return (node, rest);
        }
    }

    public class NodeVariable : INode
    {
        public string VariableName { get; set; }
        public string PropertyName { get; set; }

        public static bool HasVariable(string shard)
        {
            return shard.Contains(Token.CommandEnd);
        }

        public static (NodeVariable, string) ParseShard(string shard)
        {
            NodeVariable node = new NodeVariable();

            var splitDot = shard.Split(".", System.StringSplitOptions.RemoveEmptyEntries);

            node.VariableName = splitDot[0].Trim();

            var indexAfterCommandEnd = splitDot[1].IndexOf(Token.CommandEnd);
            var commandLength = Token.CommandEnd.Length;

            node.PropertyName = splitDot[1].Substring(0, indexAfterCommandEnd).TrimStart();

            var rest = splitDot[1].Substring(indexAfterCommandEnd + commandLength, splitDot[1].Length - indexAfterCommandEnd - commandLength);

            return (node, rest);
        }
    }
}
