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
    public class NICsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: NICs
        public ActionResult Index()
        {
            return View(db.NICs.ToList());
        }

        // GET: NICs/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NIC nIC = db.NICs.Find(id);
            if (nIC == null)
            {
                return HttpNotFound();
            }
            return View(nIC);
        }

        // GET: NICs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: NICs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "NicId,VmId")] NIC nIC)
        {
            if (ModelState.IsValid)
            {
                db.NICs.Add(nIC);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(nIC);
        }

        // GET: NICs/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NIC nIC = db.NICs.Find(id);
            if (nIC == null)
            {
                return HttpNotFound();
            }
            return View(nIC);
        }

        // POST: NICs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "NicId,VmId")] NIC nIC)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nIC).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nIC);
        }

        // GET: NICs/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NIC nIC = db.NICs.Find(id);
            if (nIC == null)
            {
                return HttpNotFound();
            }
            return View(nIC);
        }

        // POST: NICs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            NIC nIC = db.NICs.Find(id);
            db.NICs.Remove(nIC);
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
