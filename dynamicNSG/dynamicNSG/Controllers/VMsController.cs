using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using dynamicNSG.Models;

namespace dynamicNSG.Controllers
{
    public class VMsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: VMs
        public ActionResult Index()
        {
            return View(db.VMs.ToList());
        }

        // GET: VMs/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VM vM = db.VMs.Find(id);
            if (vM == null)
            {
                return HttpNotFound();
            }
            return View(vM);
        }

        // GET: VMs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: VMs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VmId,Name,OS,ResourceGroup")] VM vM)
        {
            if (ModelState.IsValid)
            {
                db.VMs.Add(vM);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(vM);
        }

        // GET: VMs/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VM vM = db.VMs.Find(id);
            if (vM == null)
            {
                return HttpNotFound();
            }
            return View(vM);
        }

        // POST: VMs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VmId,Name,OS,ResourceGroup")] VM vM)
        {
            if (ModelState.IsValid)
            {
                db.Entry(vM).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(vM);
        }

        // GET: VMs/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VM vM = db.VMs.Find(id);
            if (vM == null)
            {
                return HttpNotFound();
            }
            return View(vM);
        }

        // POST: VMs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            VM vM = db.VMs.Find(id);
            db.VMs.Remove(vM);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
