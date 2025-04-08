using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Db;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.Db
{
    /// <summary>
    /// 图像记录数据库管理：实现对图像记录数据库的增删查改。
    /// </summary>
    public class ImageRecordDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }


        public DbSet<ImageRecord> ImageRecordsSet
        {
            get { return ContextProvider.ImageRecordSet; }
        }
        /// <summary>
        /// 检索图像时
        /// </summary>
        public string CurrentAccountId { get; set; }

        /// <summary>
        /// 回拉时
        /// </summary>
        public List<string> PullBackAccountId { get; set; }

        public ImageRecordDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public ImageRecordDbSet(IScannerDbContextProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            ContextProvider = provider;
        }

        /// <summary>
        /// 添加一条新的图像记录，成功后返回被添加的图像记录
        /// 添加成功后，图像记录中的 ImageRecordId 将被自动分配一个值，即数据库自动生成的主键
        /// </summary>
        /// <param name="record"></param>
        /// <returns>返回添加成功的记录，其中的ImageRecordId将自动被分配一个值</returns>
        public ImageRecord Add(ImageRecord record)
        {
            ImageRecordsSet.Add(record);
            //Context.SaveChanges();
            SaveChanges();

            return record;
        }

        public void Update(IEnumerable<ImageRecord> records)
        {
            try
            {
                if (records != null)
                {
                    ImageRecordsSet.AddOrUpdate(records.ToArray());
                    SaveChanges();
                }
            }
            catch(Exception ex)
            {
                Tracer.TraceInfo("You Missed a Update when Mark");  //yxc
                return;
                
            }
        }

        /// <summary>
        /// 锁定指定的图像记录
        /// </summary>
        /// <param name="records">要锁定的图像记录</param>
        public static void LockImages(IEnumerable<ImageRecord> records)
        {
            var set = new ImageRecordDbSet();

            foreach (var record in records)
            {
                var r = set.ImageRecordsSet.Find(record.ImageRecordId);
                if (r != null)
                {
                    r.IsLocked = true;
                }
            }

            set.SaveChanges();
        }

        /// <summary>
        /// 解锁指定的图像记录
        /// </summary>
        /// <param name="records"></param>
        public static void UnLockImages(IEnumerable<ImageRecord> records)
        {
            var set = new ImageRecordDbSet();

            foreach (var record in records)
            {
                var r = set.ImageRecordsSet.Find(record.ImageRecordId);
                if (r != null)
                {
                    r.IsLocked = false;
                }
            }

            set.SaveChanges();
        }

        /// <summary>
        /// 获取最早的若干条图像记录。所返回的图像记录中，第一条是时间上最早的，最后一条是时间上最近的
        /// </summary>
        /// <param name="count"></param>
        /// <returns>所返回的记录数，不超过表中存储的记录总数</returns>
        public List<ImageRecord> TakeEarliest(int count)
        {
            return ImageRecordsSet.Where(r => !r.IsLocked).Take(() => count).ToList();
        }

        public ImageRecord TakeRecordByPath(string path)
        {
            var query = ImageRecordsSet.Where(imageRecord => imageRecord.StorePath == path)
                .OrderByDescending(record => record.ImageRecordId)                
              .Take(1).ToList();
            if (query !=null && query.Count > 0)
            {
                return query[0];
            }
            return null;
        }

        /// <summary>
        /// 获取最新的若干条图像记录。所返回的图像记录中，第一条是时间上最近的，最后一条是时间上最早的
        /// </summary>
        /// <param name="count"></param>
        /// <returns>所返回的记录数，不超过表中存储的记录总数</returns>
        public List<ImageRecord> TakeLatest(int count)
        {
            var query = ImageRecordsSet.OrderByDescending(record => record.ImageRecordId)
                .Where(imageRecord => imageRecord.AccountId == CurrentAccountId)
                .Take(count).ToList();
            //var str =query.ToString();
            return query;
        }

        /// <summary>
        /// 获取早于输入图像记录的若干记录.返回记录集按主键由大到小排列
        /// </summary>
        /// <param name="count">拟获取的记录的数量，实际返回的数量可能小于或等于此值</param>
        /// <param name="refRecord">用于对比的记录，返回的记录的主键比此记录的主键小</param>
        /// <returns></returns>
        public List<ImageRecord> TakeRecordsBefore(int count, ImageRecord refRecord)
        {
            var time = DateTime.Now;
            Context.Database.Log = sql => Tracer.TraceDebug($"【SQLTail】SQL:{sql.Replace("\r\n"," ")}");
            var query =
                ImageRecordsSet.Where(imageRecord => imageRecord.ImageRecordId < refRecord.ImageRecordId && PullBackAccountId.Contains(imageRecord.AccountId))
                    .OrderByDescending(imageRecord => imageRecord.ScanTime)
                    .Take(count).ToList();
            Tracer.TraceDebug($"【PullBackTimeoutTracked】sql execution time:{(DateTime.Now - time).TotalMilliseconds} Milliseconds");
            return query;
        }


        public List<ImageRecord> PullbackTakeLatest(int count)
        {
            var query = ImageRecordsSet.OrderByDescending(record => record.ImageRecordId)
                .Where(imageRecord => PullBackAccountId.Contains(imageRecord.AccountId))
                .OrderByDescending(imageRecord => imageRecord.ScanTime)
                .Take(count).ToList();
            return query;
            //if (query != null && query.Count() > 0)
            //{
            //    return query.ToList();
            //}
            //else
            //{
            //    return new List<ImageRecord>(0);
            //}
        }

        /// <summary>
        /// 获取参考记录之后的若干记录，返回记录集按照主键由小到大排列
        /// </summary>
        /// <param name="count">拟获取的记录的数量，实际返回的数量可能小于或等于此值</param>
        /// <param name="refRecord">参考记录</param>
        /// <returns></returns>
        public List<ImageRecord> TakeRecordsAfter(int count, ImageRecord refRecord)
        {
            var query = ImageRecordsSet.Where(record => record.ImageRecordId > refRecord.ImageRecordId)
                .Where(imageRecord => PullBackAccountId.Contains(imageRecord.AccountId))
                .OrderBy(imageRecord => imageRecord.ScanTime)
                .Take(count).ToList();
            return query;
        }

        /// <summary>
        /// 根据条件检索图像记录
        /// </summary>
        /// <param name="scanTimeStart"></param>
        /// <param name="scanTimeEnd"></param>
        /// <param name="accountId"></param>
        /// <param name="onlyLocked"></param>
        /// <returns></returns>
        public List<ImageRecord> TakeByConditions(DateTime scanTimeStart, DateTime scanTimeEnd, List<string> accountId,
            bool onlyLocked,bool onlyMarked)
        {
            var query =
                ImageRecordsSet.Where(record => record.ScanTime >= scanTimeStart && record.ScanTime <= scanTimeEnd).OrderByDescending(r => r.ImageRecordId).ToList();

            if (accountId!=null && accountId.Count > 0)
            {
                query = query.Where(record => accountId.Contains(record.AccountId)).ToList();
            }

            if (onlyLocked)
            {
                query = query.Where(record => record.IsLocked == true).ToList();
            }

            if (onlyMarked)
            {
                query = query.Where(record => record.IsManualSaved == true).ToList();
            }

            return query.ToList();
        }

        /// <summary>
        /// 根据条件检索图像记录
        /// </summary>
        /// <param name="scanTimeStart"></param>
        /// <param name="scanTimeEnd"></param>
        /// <param name="accountId"></param>
        /// <param name="onlyLocked"></param>
        /// <update>姜毅改</update>
        /// <returns></returns>
        public List<ImageRecord> TakeByConditions(DateTime scanTimeStart, DateTime scanTimeEnd, List<string> accountId,
            bool onlyLocked, bool onlyMarked,int pageCount,int pageSize)
        {
            string sql = "SELECT ImageRecordId,AccountId,ObjectId,StorePath,ScanTime,MachineNumber,IsLocked,IsManualSaved FROM ImageRecord Where";
            if(scanTimeStart != null)
            {
                sql += " ScanTime >= '" + scanTimeStart.ToString("yyyy-MM-dd HH:mm:ss");
                sql += "' ";
            }
            if (scanTimeEnd != null)
            {
                sql += " and ScanTime <= '" + scanTimeEnd.ToString("yyyy-MM-dd HH:mm:ss");
                sql += "' ";
            }
            if (accountId != null && accountId.Count > 0)
            {
                sql += " and AccountId in ('";
                foreach (string id in accountId)
                {
                    sql += id + "','";
                }
                sql = sql.Substring(0, sql.Length - 3);
                sql += "')";
            }
            if (onlyLocked)
                sql += " and IsLocked = 1";
            if (onlyMarked)
                sql += " and IsManualSaved = 1";
            sql += " Order by ImageRecordId desc LIMIT " + pageSize + " OFFSET "+pageCount;
            Console.WriteLine(sql);
            var list = ImageRecordsSet.SqlQuery(sql).ToList();
            return list;
        }

        //wjj
        public List<ImageRecord> TakeByNumConditions(int count, List<string> accountId,
            bool onlyLocked, bool onlyMarked)
        {
            var query =
                ImageRecordsSet.Where(record => record.AccountId!=null).Take(count);

            if (accountId != null && accountId.Count > 0)
            {
                query = query.Where(record => accountId.Contains(record.AccountId));
            }

            if (onlyLocked)
            {
                query = query.Where(record => record.IsLocked == true);
            }

            if (onlyMarked)
            {
                query = query.Where(record => record.IsManualSaved == true);
            }

            var a = query.OrderByDescending(r => r.ImageRecordId).ToList();

            return a;
        }

        public ImageRecord TakeImageRecordByScanTime(DateTime scantime,string account)
        {
            var query =
                ImageRecordsSet.Where(record => record.ScanTime == scantime);

            query = query.Where(record => record.AccountId == account);

            return query.FirstOrDefault();
        }

        public int CountByConditions(DateTime scanTimeStart, DateTime scanTimeEnd)
        {
            var query =
                ImageRecordsSet.Count(record => record.ScanTime >= scanTimeStart && record.ScanTime <= scanTimeEnd);
            return query;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scanTimeStart"></param>
        /// <param name="scanTimeEnd"></param>
        /// <param name="accountId"></param>
        /// <param name="onlyLocked"></param>
        /// <param name="onlyMarked"></param>
        /// <update>姜毅改</update>
        /// <returns></returns>       
        public int CountByConditions(DateTime scanTimeStart, DateTime scanTimeEnd, List<string> accountId,
           bool onlyLocked, bool onlyMarked)
        {
            var query = ImageRecordsSet.Where(it => it.ScanTime >= scanTimeStart && it.ScanTime <= scanTimeEnd && accountId.Contains(it.AccountId));
            if (onlyLocked)
                query.Where(it => it.IsLocked == true);
            if(onlyMarked)
                query.Where(it => it.IsManualSaved == true);
            return query.Count();
            //string sql = "SELECT count(ImageRecordId) FROM ImageRecord Where";
            //if (scanTimeStart != null)
            //{
            //    sql += " ScanTime >= '" + scanTimeStart.ToString("yyyy-MM-dd HH:mm:ss");
            //    sql += "' ";
            //}
            //if (scanTimeEnd != null)
            //{
            //    sql += " and ScanTime <= '" + scanTimeEnd.ToString("yyyy-MM-dd HH:mm:ss");
            //    sql += "' ";
            //}
            //if (accountId != null && accountId.Count > 0)
            //{
            //    sql += " and AccountId in ('";
            //    foreach (string id in accountId)
            //    {
            //        sql += id + "','";
            //    }
            //    sql = sql.Substring(0, sql.Length - 3);
            //    sql += "')";
            //}
            //if (onlyLocked)
            //    sql += " and IsLocked = 1";
            //if (onlyMarked)
            //    sql += " and IsManualSaved = 1";
            //Console.WriteLine(sql);
            //return ImageRecordsSet.SqlQuery(sql).Count();
            //if (!onlyLocked && !onlyMarked)
            //{
            //    return ImageRecordsSet.Count(record => record.ScanTime >= scanTimeStart && record.ScanTime <= scanTimeEnd && accountId.Contains(record.AccountId));

            //}

            //return ImageRecordsSet.Count(record => record.ScanTime >= scanTimeStart && record.ScanTime <= scanTimeEnd && accountId.Contains(record.AccountId)
            //    && record.IsLocked == onlyLocked && record.IsManualSaved == onlyMarked);

        }

        /// <summary>123
        /// 删除一组图像记录
        /// </summary>
        /// <param name="records"></param>
        /// <param name="commitImmediately"></param>
        public void RemoveRange(IEnumerable<ImageRecord> records, bool commitImmediately = true)
        {
            ImageRecordsSet.RemoveRange(records);

            if (commitImmediately)
            {
                SaveChanges();
            }
        }

        /// <summary>
        /// 删除单条图像记录
        /// </summary>
        /// <param name="record"></param>
        /// <param name="commitImmediately"></param>
        public void Remove(ImageRecord record, bool commitImmediately = true)
        {
            ImageRecordsSet.Remove(record);

            if (commitImmediately)
            {
                SaveChanges();
            }
        }

        /// <summary>
        /// 异步获取图像记录总数
        /// </summary>
        /// <returns>当前数据库中记录的图像记录总数</returns>
        public Task<int> CountAsync()
        {
            return ImageRecordsSet.CountAsync();
        }

        /// <summary>
        /// 获取图像记录总数
        /// </summary>
        /// <returns>当前数据库中记录的图像记录总数</returns>
        public int Count()
        {
            return ImageRecordsSet.Count();
        }

        /// <summary>
        /// 保存此前所有修改
        /// </summary>
        public void SaveChanges()
        {
            try
            {
                Context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ocException)
            {
                Tracer.TraceException(ocException);

                var objectContext = ((IObjectContextAdapter) Context).ObjectContext;
                foreach (var objectStateEntry in ocException.Entries)
                    objectContext.Refresh(RefreshMode.StoreWins, objectStateEntry.Entity);
                Context.SaveChanges();
            }
        }
    }
}
