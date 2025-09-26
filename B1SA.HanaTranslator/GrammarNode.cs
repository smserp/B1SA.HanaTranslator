using System.Collections;
using System.Reflection;
using Antlr.Runtime;

namespace B1SA.HanaTranslator
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcludeFromChildrenListAttribute : Attribute { }

    public class Note
    {
        public const string CASEFIXER = "CaseFixer";
        public const string ERR_CASEFIXER = "ErrorCaseFixer";
        public const string DEBUG_CASEFIXER = "DebugCaseFixer";
        public const string STRINGIFIER = "Stringifier";
        public const string MODIFIER = "Modifier";
        public const string ERR_MODIFIER = "ErrorModifier";

        public string ID;
        public string Value;
        public Note(string id, string value)
        {
            this.ID = id;
            this.Value = value;
        }
    }

    public class GrammarNodeInfo
    {
        public string Setter { get; private set; }
        public int Index { get; private set; }

        public GrammarNodeInfo(string setter, int index = -1)
        {
            Setter = setter;
            Index = index;
        }
    }

    public abstract class GrammarNode : ICloneable
    {
        private IList<KeyValuePair<GrammarNode, GrammarNodeInfo>> childrenList = null;
        public List<Note> TranslationNotes = [];

        // Last created grammar node. This is used to add SQL comments to the most recently parsed grammar node.
        public static GrammarNode LastGrammarNode { get; private set; }

        /// <summary>
        /// If set from outside, can override the defaults
        /// </summary>
        public Config Configuration = new();

        public GrammarNode()
        {
            LastGrammarNode = this;
            Comments = [];
        }

        // Comments that are immediately after this statement.
        public List<Comment> Comments { get; private set; }

        public virtual void Assembly(Assembler asm)
        {
            var t = GetType();
            AddNote(Note.STRINGIFIER, String.Format(Resources.TYPE_NOT_SUPPORTED_BY_STRINGIFIER, t.Name));
        }

        public void AddNote(string id, string note)
        {
            if (TranslationNotes.Find(
                  delegate (Note nt) {
                      return nt.ID == id && nt.Value == note;
                  }) == null) {
                TranslationNotes.Add(new Note(id, note));
            }
        }

        [ExcludeFromChildrenList, System.ComponentModel.DefaultValue(null)]
        public GrammarNode ReplacedNode { get; set; }

        [ExcludeFromChildrenList, System.ComponentModel.DefaultValue(false)]
        public virtual bool Hide { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public string ReturnNotes()
        {
            var ret = string.Empty;
            if (Configuration.TranslationComments) {
                var list = new List<string>();
                var ns = new NotesScanner(list);

                ns.SetFilter(Configuration.TranslationCommentsFilter.ToArray());
                ns.WriteNotesToWriter(true);

                ns.Scan(this);

                if (list.Count > 0) {
                    ret = list.Distinct().Aggregate((i, j) => i + Environment.NewLine + j);
                    ret += Environment.NewLine;
                }
            }
            return ret;
        }

        [ExcludeFromChildrenList]
        public virtual IEnumerable<KeyValuePair<GrammarNode, GrammarNodeInfo>> Children {
            get {
                //if (childrenList == null)
                {
                    CreateChildrenList();
                }

                foreach (var g in childrenList) {
                    yield return g;
                }
            }
        }

        private void CreateChildrenList()
        {
            childrenList = [];

            foreach (var property in GetType().GetProperties()) {
                var getter = property.GetGetMethod(false);
                if (getter != null && getter.GetParameters().Length == 0 && getter.GetGenericArguments().Length == 0) {
                    var attrs = property.GetCustomAttributes(typeof(ExcludeFromChildrenListAttribute), true);
                    if (attrs.Length == 0) {
                        if (typeof(GrammarNode).IsAssignableFrom(getter.ReturnType)) {
                            var objNode = (GrammarNode) getter.Invoke(this, null);
                            if (objNode != null) {
                                var setter = property.GetSetMethod(false);
                                childrenList.Add(new KeyValuePair<GrammarNode, GrammarNodeInfo>(
                                    objNode, new GrammarNodeInfo(setter.Name)));
                            }
                        }
                        else if (getter.ReturnType.IsGenericType && getter.ReturnType.GetGenericArguments().Any(
                            x => typeof(GrammarNode).IsAssignableFrom(x) || x.IsGenericType)) {
                            var objNode = getter.Invoke(this, null);
                            if (objNode != null) {
                                var setter = property.GetSetMethod(false);
                                var gnl = CreateGrammarNodeList(setter.Name, objNode);

                                if (gnl.Key != null) {
                                    childrenList.Add(gnl);
                                }
                            }
                        }
                    }
                }
            }
        }

        private KeyValuePair<GrammarNode, GrammarNodeInfo> CreateGrammarNodeList(string setterName, object objNode)
        {
            var templateType = objNode.GetType();

            if (templateType.IsGenericType) {
                var listType = CreateListItemType(templateType);

                if (listType != null) {
                    var innerListType = GetTemplateType(listType);

                    if (innerListType.IsGenericType) {
                        var innerType = GetTemplateType(innerListType);

                        var generic = typeof(List<>);
                        var lType = generic.MakeGenericType(innerListType);

                        var list = (IList) Activator.CreateInstance(lType, null);

                        var enumerable = objNode as IEnumerable;
                        if (enumerable != null) {
                            var enumerator = enumerable.GetEnumerator();
                            var hasMore = enumerator.MoveNext();
                            while (hasMore) {
                                var current = enumerator.Current;
                                hasMore = enumerator.MoveNext();

                                var innerList = (GrammarNode) Activator.CreateInstance(innerListType, current);

                                list.Add(innerList);
                            }
                        }

                        var gnl = (GrammarNode) Activator.CreateInstance(listType, list);
                        return new KeyValuePair<GrammarNode, GrammarNodeInfo>(gnl, new GrammarNodeInfo(setterName));

                    }
                    else {
                        var gnl = (GrammarNode) Activator.CreateInstance(listType, objNode);
                        return new KeyValuePair<GrammarNode, GrammarNodeInfo>(gnl, new GrammarNodeInfo(setterName));
                    }
                }
                else {
                    return new KeyValuePair<GrammarNode, GrammarNodeInfo>(null, null);
                }
            }
            return new KeyValuePair<GrammarNode, GrammarNodeInfo>(null, null);
        }

        private Type CreateListItemType(Type genericType)
        {
            if (genericType.IsGenericType) {
                var generic = typeof(GrammarNodeList<>);
                var tType = GetTemplateType(genericType);
                var finalType = CreateListItemType(tType);

                return (finalType == null) ? null : generic.MakeGenericType(finalType);
            }
            else if (typeof(GrammarNode).IsAssignableFrom(genericType)) {
                return genericType;
            }

            return null;
        }

        private Type GetTemplateType(Type template)
        {
            var argType = template.GetGenericArguments();
            return argType[0];
        }

        // Create comment nodes from comment tokens.
        public void AppendComment(bool newLine, IToken token)
        {
            var text = token.Text;
            var type = text.StartsWith("--") ? CommentType.SingleLine : CommentType.MultiLine;

            // Remove comment mark characters.
            if (type == CommentType.SingleLine) {
                text = text.Substring(2);
            }
            else {
                text = text.Substring(2, text.Length - 4);
            }

            Comments.Add(new Comment(type, newLine, text, token.TokenIndex));
        }

        public void RemoveComment(IToken token)
        {
            Comments.RemoveAll(c => c.TokenIndex == token.TokenIndex);
        }

        public void MoveCommentsFrom(GrammarNode node)
        {
            Comments.AddRange(node.Comments);
            node.Comments.Clear();
        }
    }

    public class GrammarNodeList<T> : GrammarNode where T : GrammarNode
    {
        public IList<T> List;

        public GrammarNodeList(IList<T> list)
        {
            List = list;
        }

        [ExcludeFromChildrenList]
        public override IEnumerable<KeyValuePair<GrammarNode, GrammarNodeInfo>> Children {
            get {
                var counter = 0;
                foreach (var g in List) {
                    yield return new KeyValuePair<GrammarNode, GrammarNodeInfo>(g, new GrammarNodeInfo("Add", counter++));
                }
            }
        }
    }

    public class Comment
    {
        public CommentType Type { get; set; }
        public bool NewLine { get; set; }
        public string Text { get; set; }

        [ExcludeFromChildrenList]
        public int TokenIndex { get; set; }

        public Comment(CommentType type, bool newLine, string text, int tokenIndex)
        {
            Type = type;
            NewLine = newLine;
            Text = text;
            TokenIndex = tokenIndex;
        }
    }

    public enum CommentType
    {
        SingleLine, MultiLine
    }
}
