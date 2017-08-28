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
    public class IPsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: IPs
        public ActionResult Index()
        {
            return View(db.IPs.ToList());
        }

        // GET: IPs/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            IP iP = db.IPs.Find(id);
            if (iP == null)
            {
                return HttpNotFound();
            }
            return View(iP);
        }

        // GET: IPs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: IPs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,NicId")] IP iP)
        {
            if (ModelState.IsValid)
            {
                db.IPs.Add(iP);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(iP);
        }

        // GET: IPs/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            IP iP = db.IPs.Find(id);
            if (iP == null)
            {
                return HttpNotFound();
            }
            return View(iP);
        }

        // POST: IPs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,NicId")] IP iP)
        {
            if (ModelState.IsValid)
            {
                db.Entry(iP).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(iP);
        }

        // GET: IPs/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            IP iP = db.IPs.Find(id);
            if (iP == null)
            {
                return HttpNotFound();
            }
            return View(iP);
        }

        // POST: IPs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            IP iP = db.IPs.Find(id);
            db.IPs.Remove(iP);
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
