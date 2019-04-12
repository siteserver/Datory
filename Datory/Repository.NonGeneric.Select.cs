using System.Collections.Generic;
using Datory.Utils;
using SqlKata;

namespace Datory
{
    public partial class Repository
    {
        public virtual T Get<T>(int id) where T : Entity
        {
            return id <= 0 ? null : Get<T>(Q.Where(nameof(Entity.Id), id));
        }

        public virtual T Get<T>(string guid) where T : Entity
        {
            return !Utilities.IsGuid(guid) ? null : Get<T>(Q.Where(nameof(Entity.Guid), guid));
        }

        public virtual T Get<T>(Query query = null)
        {
            var value = RepositoryUtils.GetValue<T>(Database, TableName, query);

            if (typeof(T).IsAssignableFrom(typeof(Entity)))
            {
                RepositoryUtils.SyncAndCheckGuid(Database, TableName, value as Entity);
            }

            return value;
        }

        public virtual IList<T> GetAll<T>(Query query = null)
        {
            var list = RepositoryUtils.GetValueList<T>(Database, TableName, query);

            if (typeof(T).IsAssignableFrom(typeof(Entity)))
            {
                foreach (var value in list)
                {
                    RepositoryUtils.SyncAndCheckGuid(Database, TableName, value as Entity);
                }
            }

            return list;
        }

        public virtual bool Exists(int id)
        {
            return id > 0 && Exists(Q.Where(nameof(Entity.Id), id));
        }

        public virtual bool Exists(string guid)
        {
            return Utilities.IsGuid(guid) && Exists(Q.Where(nameof(Entity.Guid), guid));
        }

        public virtual bool Exists(Query query = null)
        {
            return RepositoryUtils.Exists(Database, TableName, query);
        }

        public virtual int Count(Query query = null)
        {
            return RepositoryUtils.Count(Database, TableName, query);
        }

        public virtual int Sum(Query query = null)
        {
            return RepositoryUtils.Sum(Database, TableName, query);
        }

        public virtual int? Max(Query query = null)
        {
            return RepositoryUtils.Max(Database, TableName, query);
        }
    }
}
