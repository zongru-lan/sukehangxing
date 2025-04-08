using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    public class TipPlanandImageController
    {
        public ObservableCollection<TipPlanandImage> Plans { get; set; }

        private TipPlanandImageDbSet _dbSet;

        public TipPlanandImageController()
        {
            try
            {
                _dbSet = new TipPlanandImageDbSet();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        public ObservableCollection<TipPlanandImage> GetAllPlans()
        {
            return new ObservableCollection<TipPlanandImage>(_dbSet.SelectAll());
        }

        public void SaveChanges()
        {
            _dbSet.SaveChanges();
        }

        public void AddOrUpdate(TipPlanandImage plan)
        {
            _dbSet.AddorUpdate(plan);
        }

        public void Remove(TipPlanandImage plan)
        {
            _dbSet.Remove(plan);
        }

        public void RemoveRange(List<TipPlanandImage> plans)
        {
            _dbSet.RemoveRange(plans);
        }
    }
}
