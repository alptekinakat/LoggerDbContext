using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AlpDb.Helper
{
    public class LoggerDbContext : DbContext
    {
        public override int SaveChanges()
        {
            var list_modified = ChangeTracker.Entries().Where(p => p.State == EntityState.Modified).ToArray();
            var list_deleted = ChangeTracker.Entries().Where(p => p.State == EntityState.Deleted).ToArray();
            var list_insterted = ChangeTracker.Entries().Where(p => p.State == EntityState.Added).ToArray();
            new Thread(() =>
            {
                WriteLog(list_modified, "U");
            }).Start();
            new Thread(() =>
            {
                WriteLog(list_deleted, "D");
            }).Start();

            int r = base.SaveChanges();
            if (list_insterted.Length > 0)
            {
                var after_inserted = ChangeTracker.Entries().Where(p => list_insterted.Select(x => x.Entity).Contains(p.Entity)).ToArray();
                new Thread(() =>
                {
                    WriteLog(after_inserted, "I");
                }).Start();
            }
            return r;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var t = new Task<int>(() => this.SaveChanges());
            t.Start();
            return t;
        }

        private void WriteLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry[] entries, string operation)
        {
            DateTime now = DateTime.Now;
            foreach (var change in entries)
            {
                var entityName = change.Entity.GetType().Name;
                var d_key = new Dictionary<string, object>();
                var d_old = new Dictionary<string, object>();
                var d_new = new Dictionary<string, object>();
                foreach (var item in change.OriginalValues.Properties.Where(prop => prop.IsPrimaryKey() == true))
                {
                    d_key.Add(item.Name, change.OriginalValues[item]);
                }
                //Console.WriteLine(entityName);
                foreach (IProperty prop in change.OriginalValues.Properties)
                {
                    var obj_orj = change.OriginalValues[prop];
                    var obj_new = change.CurrentValues[prop];
                    //Console.WriteLine(prop.Name + " (ORJ):" + JsonConvert.SerializeObject(obj_orj));
                    //Console.WriteLine(prop.Name + " (CUR):" + JsonConvert.SerializeObject(obj_new));
                    if (((operation == "U") && (JsonConvert.SerializeObject(obj_new) != JsonConvert.SerializeObject(obj_orj))) || (operation != "U"))
                    {

                        d_old.Add(prop.Name, obj_orj);
                        d_new.Add(prop.Name, obj_new);
                    }
                }
                if (d_old.Count > 0)
                {
                    //You are save to file or insert another db table :)

                    Console.WriteLine("TABLE NAME :" + entityName);
                    Console.WriteLine("TABLE KEY :" + JsonConvert.SerializeObject(d_key));
                    Console.WriteLine("OPERATION :" + operation);
                    Console.WriteLine("TIME :" + now);
                    Console.WriteLine("OLD RECORD :" + (((operation == "U") || (operation == "D")) ? JsonConvert.SerializeObject(d_old) : ""));
                    Console.WriteLine("NEW RECORD :" + (((operation == "U") || (operation == "I")) ? JsonConvert.SerializeObject(d_new) : ""));
                }
            }
        }

    }
}