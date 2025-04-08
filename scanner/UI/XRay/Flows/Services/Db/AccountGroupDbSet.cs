using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using UI.XRay.Business.DataAccess.Db;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.Db
{
    public class AccountGroupDbSet
    {
        private DbSet<AccountGroup> AccountGroupSet
        {
            get { return ContextProvider.AccountGroupSet; }
        }

        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        /// <summary>
        /// 构造一个实例，同时将会创建一个新的数据库连接。
        /// </summary>
        public AccountGroupDbSet(IScannerDbContextProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            ContextProvider = provider;
        }

        public AccountGroupDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public List<AccountGroup> SelectAll()
        {
            return AccountGroupSet.Select(group => group).ToList();
        }

        public List<string> SelectGroupIDs()
        {
            return AccountGroupSet.Select(group => group.GroupID).ToList();
        }

        public List<string> SelectGroupNames()
        {
            return AccountGroupSet.Select(group => group.GroupName).ToList();
        }

        public void AddOrUpdate(AccountGroup accountGroup, bool commitImmediately = true)
        {
            AccountGroupSet.AddOrUpdate(accountGroup);
            if (commitImmediately)
            {
                Context.SaveChanges();
            }
        }

        public void AddOrUpdate(IEnumerable<AccountGroup> accountGroups, bool commitImmediately = true)
        {
            foreach (var account in accountGroups)
            {
                AccountGroupSet.AddOrUpdate(account);
            }
            if (commitImmediately)
            {
                Context.SaveChanges();
            }
        }

        public void Remove(AccountGroup accountGroup, bool commitImmediately = true)
        {
            AccountGroupSet.Remove(accountGroup);
            if (commitImmediately)
            {
                Context.SaveChanges();
            }
        }

        public void RemoveRange(IEnumerable<AccountGroup> accountGroups, bool commitImmediately = true)
        {
            AccountGroupSet.RemoveRange(accountGroups);
            if (commitImmediately)
            {
                Context.SaveChanges();
            }
        }

        public AccountGroup Find(string groupID)
        {
            return AccountGroupSet.Find(groupID);
        }

        public bool Has(string groupID)
        {
            return Find(groupID) != null;
        }

        public AccountGroup FindByName(string groupName)
        {
            var result = AccountGroupSet.Where(group => group.GroupName == groupName);
            return result.Count() < 1 ? null : result.First();
        }

        public bool HasByName(string groupName)
        {
            return FindByName(groupName) != null;
        }
    }
}
