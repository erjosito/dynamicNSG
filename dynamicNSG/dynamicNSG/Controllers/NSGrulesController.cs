using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using dynamicNSG.Models;
using dynamicNSG.Helper;

namespace dynamicNSG.Controllers
{
    public class NSGrulesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: NSGrules
        public ActionResult Index()
        {
            //return View(db.NSGrules.ToList());
            return View(dynamicNSG.Helper.NsgRuleset.buildNsgRuleset());
        }

        // GET: NSGrules/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NSGrule nSGrule = db.NSGrules.Find(id);
            if (nSGrule == null)
            {
                return HttpNotFound();
            }
            return View(nSGrule);
        }

        // GET: NSGrules/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: NSGrules/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,nsgName,direction,order,action,srcIp,srcProt,srcPort,dstIp,dstProt,dstPort")] NSGrule nSGrule)
        {
            if (ModelState.IsValid)
            {
                db.NSGrules.Add(nSGrule);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(nSGrule);
        }

        // GET: NSGrules/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NSGrule nSGrule = db.NSGrules.Find(id);
            if (nSGrule == null)
            {
                return HttpNotFound();
            }
            return View(nSGrule);
        }

        // POST: NSGrules/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,nsgName,direction,order,action,srcIp,srcProt,srcPort,dstIp,dstProt,dstPort")] NSGrule nSGrule)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nSGrule).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nSGrule);
        }

        // GET: NSGrules/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NSGrule nSGrule = db.NSGrules.Find(id);
            if (nSGrule == null)
            {
                return HttpNotFound();
            }
            return View(nSGrule);
        }

        // POST: NSGrules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            NSGrule nSGrule = db.NSGrules.Find(id);
            db.NSGrules.Remove(nSGrule);
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
