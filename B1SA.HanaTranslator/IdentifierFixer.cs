using System.Data;

namespace B1SA.HanaTranslator
{
    public class IdentifierFixer : Scanner
    {
        private enum ObjectType
        {
            COLUMN = 0,
            TABLE,
            PROCEDURE,
            INDEX,
            TYPE,
            VIEW,
            CONSTRAINT,
            TRIGGER,
            SCHEMA,
            OTHER
        };

        #region IdentifierRequest
        private class IdentifierRequest
        {
            public string Name = string.Empty;
            public List<string> Tables = [];
            public List<DbObject> Objects = [];

            public static string GetSimpleFullName(DbObject node)
            {
                return node.Identifiers.Select(s => s.Name).DefaultIfEmpty().Aggregate((aggr, item) => aggr + "." + item);
            }
        }
        #endregion

        #region ObjectNode
        private class DbObjectNode(string name, ObjectType objType, DbObject node)
        {
            public string Name = name;
            public ObjectType Type = objType;
            public DbObject Node = node;
        }
        #endregion

        private readonly List<KeyValuePair<DbObject, List<DbObjectTableSource>>> columnTables = [];
        private readonly Stack<IList<DbObjectTableSource>> tablesContext = new();
        private readonly List<DbObjectNode> tableNodes = [];
        private readonly List<DbObjectNode> procedureNodes = [];
        private readonly List<DbObjectNode> viewNodes = [];
        private readonly List<DbObject> toSkip = [];
        private readonly List<KeyValuePair<DbObject, List<DbObjectTableSource>>> indexNodes = [];
        private readonly List<KeyValuePair<DbObject, List<DbObjectTableSource>>> constraintNodes = [];
        private readonly List<DbObjectNode> typeNodes = [];
        private readonly List<DbObjectNode> triggerNodes = [];

        private readonly List<GrammarNode> querySpecifications = [];
        private readonly Config config;

        public IdentifierFixer(Config configuration)
        {
            config = configuration;
        }

        #region Handling available tables
        private void AddTablesContext(GrammarNode node)
        {
            tablesContext.Push(GetAvailTables(node));
        }

        private List<DbObjectTableSource> GetPossibleTables(string tableAlias)
        {
            var ret = new List<DbObjectTableSource>();
            if (string.IsNullOrEmpty(tableAlias)) {
                //it could be any of available tables in context
                foreach (var list in tablesContext) {
                    if (list == null) {
                        continue;
                    }

                    foreach (var ts in list) {
                        ret.Add(ts);
                    }
                }
            }
            else {
                //find the nearest same table alias
                foreach (var list in tablesContext) {
                    if (list == null) {
                        continue;
                    }

                    foreach (var ts in list) {
                        var found = ts.Alias != null && ts.Alias.Name.ToLower() == tableAlias.ToLower();
                        if (found == false) {
                            var fullName = IdentifierRequest.GetSimpleFullName(ts.DbObject);
                            if (tableAlias.ToLower() == fullName.ToLower()) {
                                found = true;
                            }
                            else {
                                if (ts.DbObject.Identifiers.Count > 1 && ts.DbObject.Identifiers[ts.DbObject.Identifiers.Count - 1].Name.ToLower() == tableAlias.ToLower()) {
                                    found = true;
                                }
                            }
                        }

                        if (found == true) {
                            ret.Add(ts);
                            return ret;
                        }
                    }
                }
            }

            return ret;
        }

        private IList<DbObjectTableSource> GetAvailTables(GrammarNode node)
        {
            IList<DbObjectTableSource> ret = [];

            switch (node) {
                case UpdateStatement us:
                    AddAvailTable(us.TableSource, ret);
                    AddAvailTables(us.FromClause, ret);
                    break;

                case DeleteStatement ds:
                    AddAvailTable(ds.Table, ret);
                    break;

                case InsertStatement ist:
                    if (ist.InsertTarget is DbObjectInsertTarget doit) {
                        AddAvailTable(doit.TableSource, ret);
                    }
                    break;

                case CreateIndexStatement cis:
                    if (cis.IndexTarget is DbObjectIndexTarget doxt) {
                        AddAvailTable(doxt.TableSource, ret);
                    }
                    break;

                case DropIndexStatement dis:
                    foreach (var action in dis.Actions) {
                        if (action.TableSource != null) {
                            AddAvailTable(action.TableSource, ret);
                        }
                    }
                    break;

                case AlterIndexStatement ais:
                    if (ais.TableSource != null) {
                        // It will be removed in Modifier, so probably useless code
                        AddAvailTable(ais.TableSource, ret);
                    }
                    break;

                case AlterTableStatement ats:
                    if (ats.TableSource != null) {
                        AddAvailTable(ats.TableSource, ret);
                    }
                    break;

                case DropTableStatement dts:
                    foreach (var tableSource in dts.TableSources) {
                        if (tableSource != null) {
                            AddAvailTable(tableSource, ret);
                        }
                    }
                    break;
            }

            TrimTables(ref ret);

            return ret.Count == 0 ? null : ret;
        }

        private void TrimTables(ref IList<DbObjectTableSource> ret)
        {
            for (var i = 0; i < ret.Count; i++) {
                var table = ret[i];
                if (table.Alias != null) {
                    for (var j = 0; j < ret.Count; j++) {
                        var source = ret[j];
                        for (var k = 0; k < source.DbObject.Identifiers.Count; k++) {
                            var id = source.DbObject.Identifiers[k];
                            if (id.Name.ToLower() == table.Alias.Name.ToLower()) {
                                ret.Remove(source);
                                j--;
                                i--;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void TrimTables(List<DbObjectNode> mTableNodes)
        {
            foreach (var source in columnTables) {
                foreach (var alias in source.Value) {
                    if (alias.Alias != null) {
                        foreach (var obj in mTableNodes) {
                            if (obj.Name == alias.Alias.Name) {
                                mTableNodes.RemoveAt(mTableNodes.IndexOf(obj));
                                return;
                            }
                        }
                    }
                }
            }
        }

        private bool AddAvailTables(IList<TableSource> fromClause, IList<DbObjectTableSource> list)
        {
            if (fromClause == null) {
                return false;
            }

            foreach (var tableSource in fromClause) {
                AddAvailTable(tableSource, list);
            }

            return true;
        }

        private bool AddAvailTable(GrammarNode node, IList<DbObjectTableSource> list)
        {
            //empty place holder for type we don't support
            return false;
        }

        private bool AddAvailTable(DbObjectTableSource node, IList<DbObjectTableSource> list)
        {
            list.Add(node);
            return true;
        }

        private bool AddAvailTable(NestedParensTableSource node, IList<DbObjectTableSource> list)
        {
            return AddAvailTable(node.TableSource, list);
        }

        private bool AddAvailTable(JoinedTableSource joinedTable, IList<DbObjectTableSource> list)
        {
            AddAvailTable(joinedTable.RightTableSource, list);
            AddAvailTable(joinedTable.LeftTableSource, list);

            return true;
        }

        private IList<DbObjectTableSource> GetAvailTables(QuerySpecification query)
        {
            IList<DbObjectTableSource> ret = [];
            AddAvailTables(query.FromClause, ret);

            return ret.Count == 0 ? null : ret;
        }

        private IList<DbObjectTableSource> GetAvailTables(OperatorQueryExpression query)
        {
            return null;
        }

        #endregion

        #region AlterIndexStatement
        public virtual bool Action(AlterIndexStatement node)
        {
            AddTablesContext(node);
            FixIdentifier(new DbObject(node.Index), ObjectType.INDEX, node.Index);
            return false;
        }
        #endregion

        #region DropIndexStatement
        public virtual bool Action(DropIndexStatement node)
        {
            AddTablesContext(node);
            if (node.Actions.Count > 0) {
                FixIdentifier(new DbObject(node.Actions[0].Index), ObjectType.INDEX, node.Actions[0].Index);
            }
            return false;
        }
        #endregion

        public virtual bool Action(DropViewStatement stmt)
        {
            if (stmt.Views.Count > 0) {
                FixIdentifiers(stmt.Views[0], ObjectType.VIEW, stmt.Views[0].Identifiers);
            }
            return false;
        }

        public virtual bool Action(AlterViewStatement stmt)
        {
            FixIdentifiers(stmt.Name, ObjectType.VIEW, stmt.Name.Identifiers);
            return false;
        }

        public virtual bool Action(CreateViewStatement stmt)
        {
            FixIdentifiers(stmt.Name, ObjectType.OTHER, stmt.Name.Identifiers);
            toSkip.Add(stmt.Name);
            return true;
        }

        public virtual bool Action(CreateDmlTriggerStatement stmt)
        {
            FixIdentifiers(stmt.Name, ObjectType.OTHER, stmt.Name.Identifiers);
            FixIdentifiers(stmt.Table, ObjectType.TABLE, stmt.Table.Identifiers);
            toSkip.Add(stmt.Name);
            toSkip.Add(stmt.Table);
            return true;
        }

        public virtual bool Action(DropTriggerStatement stmt)
        {
            FixIdentifiers(stmt.Name, ObjectType.TRIGGER, stmt.Name.Identifiers);
            return false;
        }

        public virtual bool Action(CreateTableTypeStatement node)
        {
            FixIdentifiers(node.Name, ObjectType.OTHER, node.Name.Identifiers);
            toSkip.Add(node.Name);
            return true;
        }

        public virtual bool Action(CreateBaseTypeStatement node)
        {
            FixIdentifiers(node.Name, ObjectType.TYPE, node.Name.Identifiers);
            return false;
        }

        public virtual bool Action(DropTypeStatement node)
        {
            FixIdentifiers(node.Name, ObjectType.TYPE, node.Name.Identifiers);
            return false;
        }

        public virtual bool Action(SetIdentityInsertStatement stmt)
        {
            FixIdentifiers(stmt.DBObject, ObjectType.TABLE, stmt.DBObject.Identifiers);
            return false;
        }

        public virtual bool Action(IdentifierSelectAlias alias)
        {
            alias.Identifier.Type = IdentifierType.Quoted;
            return false;
        }

        public virtual bool Action(OrderedColumn col)
        {
            col.Name.Type = IdentifierType.Quoted;
            return false;
        }

        public virtual bool Action(ColumnDefinition node)
        {
            // There is only one Identifier, and therefore there should be no problem with removing
            FixIdentifier(new DbObject(node.Name), ObjectType.OTHER, node.Name);
            return false;
        }

        public virtual bool Action(ComputedColumnDefinition node)
        {
            FixIdentifier(new DbObject(node.Name), ObjectType.OTHER, node.Name);
            return false;
        }

        public virtual bool Action(CreateTableStatement node)
        {
            FixIdentifiers(node.Name, ObjectType.OTHER, node.Name.Identifiers);
            toSkip.Add(node.Name);
            return true;
        }

        public virtual bool Action(ForeignKeyColumnConstraint node)
        {
            //todo: this should be cleared in modifier, imho
            // Unsupported grammar node on HANA - nothing to check
            return false;
        }

        public virtual bool Action(ForeignKeyTableConstraint node)
        {
            //todo: this should be cleared in modifier, imho
            // Unsupported grammar node on HANA - nothing to check
            return false;
        }

        public virtual bool Action(PrimaryKeyTableConstraint node)
        {
            // IdentifierObject.OTHER is used, no correction for ADD/CREATE
            if (node.Name != null) {
                FixIdentifier(new DbObject(node.Name), ObjectType.OTHER, node.Name);
            }
            foreach (var oc in node.Columns) {
                FixIdentifier(new DbObject(oc.Name), ObjectType.OTHER, oc.Name);
            }
            return false;
        }

        public virtual bool Action(CreateIndexStatement stmt)
        {
            AddTablesContext(stmt);

            FixIdentifier(new DbObject(stmt.Name), ObjectType.OTHER, stmt.Name);
            FixIdentifiers(stmt.IndexTarget.TableSource.DbObject, ObjectType.TABLE, stmt.IndexTarget.TableSource.DbObject.Identifiers);
            foreach (var col in stmt.IndexColumns) {
                FixIdentifier(new DbObject(col.Name), ObjectType.COLUMN, col.Name);
            }
            return false;
        }

        public virtual bool Action(InsertStatement node)
        {
            AddTablesContext(node);

            if (node.ColumnList != null) {
                foreach (var column in node.ColumnList) {
                    FixIdentifier(new DbObject(column), ObjectType.COLUMN, column);
                }
            }

            return true;
        }

        public virtual bool Action(CreateProcedureStatement node)
        {
            FixIdentifiers(node.Name, ObjectType.OTHER, node.Name.Identifiers);
            toSkip.Add(node.Name);
            return true;
        }

        public virtual bool Action(DropProcedureStatement node)
        {
            if (node.Names.Count > 0) {
                FixIdentifiers(node.Names[0], ObjectType.PROCEDURE, node.Names[0].Identifiers);
            }

            return false;
        }

        public virtual bool Action(DbObjectInsertTarget node)
        {
            FixIdentifiers(node.TableSource.DbObject, ObjectType.TABLE, node.TableSource.DbObject.Identifiers);
            return false;
        }

        #region AlterTableStatement
        public virtual bool Action(AlterTableStatement node)
        {
            AddTablesContext(node);
            FixIdentifiers(node.TableSource.DbObject, ObjectType.TABLE, node.TableSource.DbObject.Identifiers);

            Action(node.Action);

            return false;
        }

        public virtual bool Action(AlterColumnAddDropAlterTableAction action)
        {
            if (action.Action == AddOrDrop.Drop) {
                FixIdentifier(new DbObject(action.Name), ObjectType.COLUMN, action.Name);
            }
            else {
                FixIdentifier(new DbObject(action.Name), ObjectType.OTHER, action.Name);
            }
            return false;
        }

        public virtual bool Action(AlterColumnDefineAlterTableAction action)
        {
            FixIdentifier(new DbObject(action.Name), ObjectType.OTHER, action.Name);
            return false;
        }

        public virtual bool Action(DropColumnAlterTableDefinition action)
        {
            FixIdentifier(new DbObject(action.Name), ObjectType.COLUMN, action.Name);
            return false;
        }

        public virtual bool Action(DropConstraintAlterTableDefinition action)
        {
            FixIdentifier(new DbObject(action.Name), ObjectType.CONSTRAINT, action.Name);
            return false;
        }

        public virtual bool Action(AddAlterTableAction node)
        {
            foreach (var cd in node.Definitions) {
                if (cd is ColumnDefinition) {
                    Action(cd as ColumnDefinition);
                }

                if (cd is PrimaryKeyTableConstraint) {
                    Action(cd as PrimaryKeyTableConstraint);
                }
            }
            return false;
        }

        public virtual bool Action(DropAlterTableAction node)
        {
            foreach (var dropDef in node.Definitions) {
                Action(dropDef);
            }
            return false;
        }
        #endregion

        public virtual bool Action(DbObjectExecModuleName node)
        {
            FixIdentifiers(node.Name, ObjectType.PROCEDURE, node.Name.Identifiers);
            return false;
        }

        #region Statement
        public virtual bool Action(Statement node)
        {
            if (node is UpdateStatement || node is DeleteStatement || node is DropTableStatement) {
                AddTablesContext(node);
            }

            return true;
        }
        #endregion

        public virtual bool Action(QuerySpecification node)
        {
            //To cover nested subqueries in statements
            //For 'standard' queries we use Action for SelectStatement
            if (querySpecifications.Contains(node) == false) {
                AddTablesContext(node);
            }
            return true;
        }

        public virtual bool Action(SelectStatement node)
        {
            //Add context table on SelectStatement level to cover Group by and Order by
            AddTablesContext(node.QueryExpression);
            querySpecifications.Add(node.QueryExpression);
            return true;
        }

        public virtual bool Action(SqlStartStatement node)
        {
            return false;
        }

        public virtual bool Action(DbObjectTableSource source)
        {
            FixIdentifiers(source.DbObject, ObjectType.TABLE, source.DbObject.Identifiers);
            return false;
        }

        public virtual bool Action(GenericDataType type)
        {
            if (type.Schema != null) {
                type.Schema.Type = IdentifierType.Quoted;
                if (type.Name != null) {
                    type.Name.Type = IdentifierType.Quoted;
                }
            }
            else {
                type.Name.Type = IdentifierType.Plain;
            }
            return false;
        }

        public virtual bool Action(DbObject dbObject)
        {
            if (toSkip.Contains(dbObject) == false) {
                FixIdentifiers(dbObject, ObjectType.COLUMN, dbObject.Identifiers);
            }
            return false;
        }

        public virtual bool Action(DbObjectExpression expression)
        {
            return Action(expression.Value);
        }

        #region Handle DBObject to Node list for scanning
        private List<KeyValuePair<DbObject, List<DbObjectTableSource>>> GetDbObjectTableNodeListByType(ObjectType type)
        {
            return type switch {
                ObjectType.COLUMN => columnTables,
                ObjectType.INDEX => indexNodes,
                ObjectType.CONSTRAINT => constraintNodes,
                _ => null,
            };
        }

        private List<DbObjectNode> GetDbObjectNodeListByType(ObjectType type)
        {
            return type switch {
                ObjectType.TRIGGER => triggerNodes,
                ObjectType.TABLE => tableNodes,
                ObjectType.TYPE => typeNodes,
                ObjectType.VIEW => viewNodes,
                ObjectType.PROCEDURE => procedureNodes,
                _ => null,
            };
        }

        private bool CheckAndQuoteIdentifiers(IList<Identifier> identifiers, ObjectType type, string objName, DbObject node)
        {
            var id = identifiers.DefaultIfEmpty().Last();
            if (id != null) {
                if (type == ObjectType.OTHER) {
                    //id.AutoChangeType();
                }
            }
            else {
                AddNote(node, Note.DEBUG_CASEFIXER, "Object " + type.ToString() + " '" + objName + "' has no identifiers");
                return false;
            }

            return true;
        }

        private void AddDbObjectToNodesList(IList<Identifier> identifiers, ObjectType type, string objName, DbObject node)
        {
            if (CheckAndQuoteIdentifiers(identifiers, type, objName, node)) {
                var list = GetDbObjectNodeListByType(type);

                if (list != null) {
                    var objNode = new DbObjectNode(objName, ObjectType.TYPE, node as DbObject);
                    list.Add(objNode);
                }
            }
        }

        private void AddDbObjectToTableNodesList(IList<Identifier> identifiers, ObjectType type, string objName, DbObject node)
        {
            if (CheckAndQuoteIdentifiers(identifiers, type, objName, node)) {
                var list = GetDbObjectTableNodeListByType(type);
                if (list != null) {
                    var table = objName.LastIndexOf('.') > 0 ? objName.Substring(0, objName.LastIndexOf('.')) : string.Empty;
                    list.Add(new KeyValuePair<DbObject, List<DbObjectTableSource>>(node, GetPossibleTables(table)));
                }
            }
        }

        #endregion

        #region FixIdentifier
        private void FixIdentifier(DbObject node, ObjectType type, Identifier identifier)
        {
            FixIdentifiers(node, type, [identifier]);
        }

        private void FixIdentifiers(DbObject node, ObjectType type, IList<Identifier> identifiers)
        {
            var objName = string.Empty;
            for (var i = 0; i < identifiers.Count; i++) {
                var id = identifiers[i];

                // remove empty identifiers
                if (string.IsNullOrEmpty(id.Name)) {
                    identifiers.RemoveAt(i--);
                    continue;
                }

                // remove "dbo" if asked for
                if (id.Name is "dbo" or "[dbo]") {
                    if (config.RemoveDboSchema) {
                        identifiers.RemoveAt(i--);
                        continue;
                    }
                    else {
                        id.Type = IdentifierType.Quoted;
                    }
                }

                objName += id.Name + ".";
            }

            objName = objName.TrimEnd('.');

            switch (type) {
                case ObjectType.COLUMN:
                case ObjectType.CONSTRAINT:
                case ObjectType.INDEX:
                    AddDbObjectToTableNodesList(identifiers, type, objName, node);
                    break;

                case ObjectType.TYPE:
                case ObjectType.VIEW:
                case ObjectType.TRIGGER:
                case ObjectType.PROCEDURE:
                case ObjectType.OTHER:
                    AddDbObjectToNodesList(identifiers, type, objName, node);
                    break;

                case ObjectType.TABLE:
                    if (objName.ToUpper() != "DUMMY") {
                        AddDbObjectToNodesList(identifiers, type, objName, node);
                        TrimTables(tableNodes);
                    }
                    break;
            }

            AddNote(node, Note.DEBUG_CASEFIXER, "Object " + type.ToString() + " '" + objName + "' cought");
        }

        private void AddNote(DbObject node, string id, string note)
        {
            if (node != null) {
                if (node.Identifiers.Last() != null) {
                    node.Identifiers.Last().AddNote(id, note);
                }
                else {
                    node.AddNote(id, note);
                }
            }
        }
        #endregion
    }
}
