﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Inventory.Models;
using Inventory.Requests;

namespace Inventory.Controllers
{
    public class UsersController : Controller
    {
        private InventoryEntities db = new InventoryEntities();

        // GET: Users
        [RoleFilter(new int[] { 1})]
        public ActionResult Index()
        {
            var users = db.Users.Include(u => u.Department);
            return View(users.ToList());
        }

        public ActionResult GetData()
        {
            using (InventoryEntities db = new InventoryEntities())
            {
                List<User> usrList = db.Users.ToList<User>();
                return Json(new { data = usrList }, JsonRequestBehavior.AllowGet);
            }
        }


        // GET: Users/Details/5
        [RoleFilter(new int[] { 1 })]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
        // GET: Users/ChangePassword
        [LoginFilter]
        [HttpGet]
        public ActionResult ChangePassword()
        {
                     
            return View();
        }
        // POST: Users/ChangePassword
        [LoginFilter]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordRequest request)
        {
            if (ModelState.IsValid)
            {
                if (BLL.SecurityHelper.MD5(request.curPassword) != UserSession.User.Password)
                {

                    ModelState.AddModelError("curPassword","Incorrect current password!");
                    return View();
                }

                if (request.newPassword != request.conPassword)
                {
                    ModelState.AddModelError("conPassword", "Password and confirm password not match!");

                    return View();
                }

                var u = db.Users.Find(UserSession.User.Usr_Id);

                u.Password = BLL.SecurityHelper.MD5(request.newPassword);

                db.Entry(u).State = EntityState.Modified;

                try
                {
                    if (db.SaveChanges() < 0)
                    {
                        ViewBag.error = "Password and confirm password not match!";
                        return View();
                    }
                    ViewBag.message = "Your password changed successfully";
                    return View();
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.error = ex.Message;
                    return View();

                }
                
            }

            return View();
        }

        // GET: Users/Create
        [RoleFilter(new int[] { 1 })]
        public ActionResult Create()
        {
            ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam");
            ViewBag.Usr_Type = new SelectList(new[] { new { ID = 1, Name = "Admin" }, new { ID = 2, Name = "User" }, new { ID = 3, Name = "Employee" } }, "ID", "Name",3);

            return View(new User());
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleFilter(new int[] { 1 })]
        public ActionResult Create([Bind(Include = "Usr_Id,Usr_Type,F_Name,L_Name,Pho_Num,Dep_Id,Email")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                var password = BLL.SecurityHelper.GeneratePassword();

                user.Password = BLL.SecurityHelper.MD5(password);
                user.Created_At = DateTime.Now;
                user.Created_By = UserSession.User.F_Name + " " + UserSession.User.L_Name;
               
                user.Status = true;
                try
                {
                    db.SaveChanges();
                    BLL.EmailHelper.SendMail(user.Email, "Register", "<strong>Dear, " + user.F_Name + " " + user.L_Name + "</strong><br/>Your account just created has " + (user.Usr_Type==1?"Admin":(user.Usr_Type==2?"User":"Employee")) + " privilege and password (<span color='red'>"+password+"</span>)<br/>Thank you, <br/> ");

                }
                catch (Exception ex )
                {

                    throw ex;
                }
                return RedirectToAction("Index");
            }

            ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam", user.Dep_Id);
            return View(user);
        }

        // GET: Users/Edit/5
        [RoleFilter(new int[] { 1 })]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam", user.Dep_Id);
            ViewBag.Usr_Type = new SelectList(new[] { new {ID=1,Name="Admin" }, new { ID = 2, Name = "User" }, new { ID = 3, Name = "Employee" } }, "ID", "Name", user.Usr_Type);
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleFilter(new int[] { 1 })]
        public ActionResult Edit([Bind(Include = "Usr_Id,Usr_Type,F_Name,L_Name,Pho_Num,Dep_Id")] User user)
        {
            if (ModelState.IsValid)
            {
                var dbUser=db.Users.Find(user.Usr_Id);//.State = EntityState.Modified;
               
                    dbUser.Usr_Id = user.Usr_Id;
                    dbUser.Usr_Type = user.Usr_Type;
                    dbUser.F_Name = user.F_Name;
                    dbUser.L_Name = user.L_Name;
                    dbUser.Pho_Num = user.Pho_Num;
                    dbUser.Dep_Id = user.Dep_Id;
                    //dbUser.Status = user.Status;
                 
                dbUser.Updated_At = DateTime.Now;
                dbUser.Updated_By = UserSession.User.F_Name + " " + UserSession.User.L_Name;

                try
                {
                    db.SaveChanges();

                }
                catch (Exception ex)
                {

                    throw ex;
                }

                return RedirectToAction("Index");
            }
            ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam", user.Dep_Id);
            return View(user);
        }
        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleFilter(new int[] { 1 })]
        public ActionResult SendNewPassword(int id)
        {
            
            var user = db.Users.Find(id);//.State = EntityState.Modified;
            var password= BLL.SecurityHelper.GeneratePassword(11);
            user.Password = BLL.SecurityHelper.MD5(password);

            user.Updated_At = DateTime.Now;
            user.Updated_By = UserSession.User.F_Name + " " + UserSession.User.L_Name;

            try
            {
                db.SaveChanges();

                BLL.EmailHelper.SendMail(user.Email, "New Password", "<strong>Dear, " + user.F_Name + " " + user.L_Name + "</strong><br/>Your password changed to (<span color='red'>" + password + "</span>).<br/>Thank you, <br/> ");

            }
            catch (Exception ex)
            {

                throw ex;
            }

             
            ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam", user.Dep_Id);
            return RedirectToAction("Edit",new { id=user.Usr_Id});
        }

        // POST: Users/Activate/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleFilter(new int[] { 1 })]
        public ActionResult Activate(int id)
        {
            var user = db.Users.Find(id);//.State = EntityState.Modified;
            

                user.Status = true;

                user.Updated_At = DateTime.Now;
                user.Updated_By = UserSession.User.F_Name + " " + UserSession.User.L_Name;

                try
                {
                    db.SaveChanges();
                    BLL.EmailHelper.SendMail(user.Email, "Account Activation", "<strong>Dear, " + user.F_Name + " " + user.L_Name + "</strong><br/>Your account just activated.<br/>Thank you, <br/>");

                }
            catch (Exception ex)
                {
                    throw ex;
                }

        
           
            ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam", user.Dep_Id);
            return RedirectToAction("Edit", new { id = user.Usr_Id });
        }
        // POST: Users/DeActivate/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleFilter(new int[] { 1 })]
        public ActionResult DeActivate(int id)
        {
            var user = db.Users.Find(id);//.State = EntityState.Modified;
            
                user.Status = false;

                user.Updated_At = DateTime.Now;
                user.Updated_By = UserSession.User.F_Name + " " + UserSession.User.L_Name;

                try
                {
                    db.SaveChanges();
                BLL.EmailHelper.SendMail(user.Email, "Account Activation", "<strong>Dear, " + user.F_Name + " " + user.L_Name + "</strong><br/>Sorry, Your account just suspended.<br/>Thank you, <br/>");

            }
            catch (Exception ex)
                {
                    throw ex;
                }
                ViewBag.Dep_Id = new SelectList(db.Departments, "Dep_Id", "Dep_Nam", user.Dep_Id);
                return RedirectToAction("Edit", new { id = user.Usr_Id });
          

        }

        // GET: Users/Delete/5
        [RoleFilter(new int[] { 1 })]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RoleFilter(new int[] { 1 })]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
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

        [HttpGet]
        public ActionResult Login(string next="/")
        {
            return View();
        }

        [HttpPost, ActionName("Login")]
        [GRecaptcha]
        [ValidateAntiForgeryToken]
        public ActionResult postLogin(Requests.LoginRequest request, string next = "/")
        {
          /* if(!ModelState.IsValid)
            {
                ViewBag.error = ModelState.First().Value.Errors.Last().ErrorMessage;
                return View();
            }*/
            if (!Regex.IsMatch(request.Email, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"))
            {
                ViewBag.error = "Invalid Email Format !";
                return View();
            }
            //var user = db.Users.Where(a => a.Email == request.Email).FirstOrDefault();
            //if (user == null || user.Password != request.Password)
            //{
            //    ViewBag.error = "Invalid User Data !";
            //    return View();
            //}



            //Session.RemoveAll();

            //Session.Add("USER_ID", user.Usr_Id);
            var user = UserSession.Login(request.Email, BLL.SecurityHelper.MD5(request.Password),request.Remember).Result;
            if (user == null)
            {
                ViewBag.error = "Invalid User Data !";
                return View();
            }


            return Redirect(next);
        }
        [HttpGet]
        public ActionResult ForgetPassword()
        {
            return View();
        }
        [HttpPost, ActionName("ForgetPassword")]
        [ValidateAntiForgeryToken]
        public ActionResult postForgetPassword()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Logout()
        {
            UserSession.Logout();
            return Redirect("/Users/Login");
        }

        [LoginFilter]
        [HttpPost]
        public ActionResult SuggestPassword()
        {
            return Content( BLL.SecurityHelper.GeneratePassword(),"text/plain");
        }

    }
}
