namespace Antlr.Runtime.Tree
{
    using System.Collections.Generic;
    using StringBuilder = System.Text.StringBuilder;

    /** A utility class to generate DOT diagrams (graphviz) from
     *  arbitrary trees.  You can pass in your own templates and
     *  can pass in any kind of tree or use Tree interface method.
     *  I wanted this separator so that you don't have to include
     *  ST just to use the org.antlr.runtime.tree.* package.
     *  This is a set of non-static methods so you can subclass
     *  to override.  For example, here is an invocation:
     *
     *      CharStream input = new ANTLRInputStream(System.in);
     *      TLexer lex = new TLexer(input);
     *      CommonTokenStream tokens = new CommonTokenStream(lex);
     *      TParser parser = new TParser(tokens);
     *      TParser.e_return r = parser.e();
     *      Tree t = (Tree)r.tree;
     *      System.out.println(t.toStringTree());
     *      DOTTreeGenerator gen = new DOTTreeGenerator();
     *      StringTemplate st = gen.toDOT(t);
     *      System.out.println(st);
     */
    public class DotTreeGenerator
    {
        private readonly string[] HeaderLines =
            {
                "digraph {",
                "",
                "\tordering=out;",
                "\tranksep=.4;",
                "\tbgcolor=\"lightgrey\"; node [shape=box, fixedsize=false, fontsize=12, fontname=\"Helvetica-bold\", fontcolor=\"blue\"",
                "\t\twidth=.25, height=.25, color=\"black\", fillcolor=\"white\", style=\"filled, solid, bold\"];",
                "\tedge [arrowsize=.5, color=\"black\", style=\"bold\"]",
                ""
            };
        private const string Footer = "}";
        private const string NodeFormat = "  {0} [label=\"{1}\"];";
        private const string EdgeFormat = "  {0} -> {1} // \"{2}\" -> \"{3}\"";

        /** Track node to number mapping so we can get proper node name back */
        private Dictionary<object, int> nodeToNumberMap = [];

        /** Track node number so we can get unique node names */
        private int nodeNumber = 0;

        /** Generate DOT (graphviz) for a whole tree not just a node.
         *  For example, 3+4*5 should generate:
         *
         * digraph {
         *   node [shape=plaintext, fixedsize=true, fontsize=11, fontname="Courier",
         *         width=.4, height=.2];
         *   edge [arrowsize=.7]
         *   "+"->3
         *   "+"->"*"
         *   "*"->4
         *   "*"->5
         * }
         *
         * Takes a Tree interface object.
         */
        public virtual string ToDot(object tree, ITreeAdaptor adaptor)
        {
            var builder = new StringBuilder();
            foreach (var line in HeaderLines)
                builder.AppendLine(line);

            nodeNumber = 0;
            var nodes = DefineNodes(tree, adaptor);
            nodeNumber = 0;
            var edges = DefineEdges(tree, adaptor);

            foreach (var s in nodes)
                builder.AppendLine(s);

            builder.AppendLine();

            foreach (var s in edges)
                builder.AppendLine(s);

            builder.AppendLine();

            builder.AppendLine(Footer);
            return builder.ToString();
        }

        public virtual string ToDot(ITree tree)
        {
            return ToDot(tree, new CommonTreeAdaptor());
        }
        protected virtual IEnumerable<string> DefineNodes(object tree, ITreeAdaptor adaptor)
        {
            if (tree == null)
                yield break;

            var n = adaptor.GetChildCount(tree);
            if (n == 0) {
                // must have already dumped as child from previous
                // invocation; do nothing
                yield break;
            }

            // define parent node
            yield return GetNodeText(adaptor, tree);

            // for each child, do a "<unique-name> [label=text]" node def
            for (var i = 0; i < n; i++) {
                var child = adaptor.GetChild(tree, i);
                yield return GetNodeText(adaptor, child);
                foreach (var t in DefineNodes(child, adaptor))
                    yield return t;
            }
        }

        protected virtual IEnumerable<string> DefineEdges(object tree, ITreeAdaptor adaptor)
        {
            if (tree == null)
                yield break;

            var n = adaptor.GetChildCount(tree);
            if (n == 0) {
                // must have already dumped as child from previous
                // invocation; do nothing
                yield break;
            }

            var parentName = "n" + GetNodeNumber(tree);

            // for each child, do a parent -> child edge using unique node names
            var parentText = adaptor.GetText(tree);
            for (var i = 0; i < n; i++) {
                var child = adaptor.GetChild(tree, i);
                var childText = adaptor.GetText(child);
                var childName = "n" + GetNodeNumber(child);
                yield return string.Format(EdgeFormat, parentName, childName, FixString(parentText), FixString(childText));
                foreach (var t in DefineEdges(child, adaptor))
                    yield return t;
            }
        }

        protected virtual string GetNodeText(ITreeAdaptor adaptor, object t)
        {
            var text = adaptor.GetText(t);
            var uniqueName = "n" + GetNodeNumber(t);
            return string.Format(NodeFormat, uniqueName, FixString(text));
        }

        protected virtual int GetNodeNumber(object t)
        {
            int i;
            if (nodeToNumberMap.TryGetValue(t, out i)) {
                return i;
            }
            else {
                nodeToNumberMap[t] = nodeNumber;
                nodeNumber++;
                return nodeNumber - 1;
            }
        }

        protected virtual string FixString(string text)
        {
            if (text != null) {
                text = System.Text.RegularExpressions.Regex.Replace(text, "\"", "\\\\\"");
                text = System.Text.RegularExpressions.Regex.Replace(text, "\\t", "    ");
                text = System.Text.RegularExpressions.Regex.Replace(text, "\\n", "\\\\n");
                text = System.Text.RegularExpressions.Regex.Replace(text, "\\r", "\\\\r");

                if (text.Length > 20)
                    text = text.Substring(0, 8) + "..." + text.Substring(text.Length - 8);
            }

            return text;
        }
    }
}
