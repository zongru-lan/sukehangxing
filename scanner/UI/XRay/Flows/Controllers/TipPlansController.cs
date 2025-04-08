using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// Tip计划的控制器：添加、删除、修改
    /// </summary>
    public class TipPlansController
    {
        public ObservableCollection<TipPlan> Plans { get; set; }

        private TipPlanDbSet _dbSet;

        public TipPlansController()
        {
            try
            {
                _dbSet = new TipPlanDbSet();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        public ObservableCollection<TipPlan> GetAllPlans()
        {
            return new ObservableCollection<TipPlan>(_dbSet.SelectAll());
        }

        public void SaveChanges()
        {
            _dbSet.SaveChanges();
        }

        public void AddOrUpdate(TipPlan plan)
        {
            _dbSet.AddorUpdate(plan);
        }

        public void Remove(TipPlan plan)
        {
            _dbSet.Remove(plan);
        }
    }
}
