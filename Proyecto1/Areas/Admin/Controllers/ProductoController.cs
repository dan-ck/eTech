﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto1.Data.Repositorio.IRepositorio;
using Proyecto1.Models;
using Proyecto1.Models.ViewModels;
using Proyecto1.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = DS.Role_Admin + "," + DS.Role_Inventario)] //- Añadir authorization when ready - Martes 16
    //Added authorization - Martes 17
    public class ProductoController : Controller
    {
        private readonly IUnidadTrabajo _unidadTrabajo;
        private readonly IWebHostEnvironment _hostEnvironment;
        public ProductoController(IUnidadTrabajo unidadTrabajo,
            IWebHostEnvironment hostEnvironment)
        {
            _unidadTrabajo = unidadTrabajo;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Upsert(int? id)
        {
            ProductoVM productoVM = new ProductoVM()
            {
                Producto = new Producto(),
                CategoriaLista = _unidadTrabajo.Categoria.ObtenerTodos().Select(c => new SelectListItem
                {
                    Text = c.Nombre,
                    Value = c.id.ToString()
                }),
                MarcaLista = _unidadTrabajo.Marca.ObtenerTodos().Select(m => new SelectListItem
                {
                    Text = m.Nombre,
                    Value = m.id.ToString()
                }),
                PadreLista = _unidadTrabajo.Producto.ObtenerTodos().Select(p => new SelectListItem
                {
                    Text = p.Descripcion,
                    Value = p.Id.ToString()
                })

            };


            if (id == null)
            {
                // Esto es para Crear nuevo Registro
                return View(productoVM);
            }
            // Esto es para Actualizar
            productoVM.Producto = _unidadTrabajo.Producto.Obtener(id.GetValueOrDefault());
            if (productoVM.Producto == null)
            {
                return NotFound();
            }

            return View(productoVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductoVM productoVM)
        {
            if (ModelState.IsValid)
            {

                // Cargar Imagenes
                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    /*
                     * El método Guid (Identificador único global) puede crear nuevos objetos como identificadores, 
                     * mediante la propiedad NewGuid 
                     * GUID (o UUID) es un acrónimo de 'Identificador global único' (o 'Identificador universal único'). 
                     * Es un número entero de 128 bits que se utiliza para identificar recursos.
                    */
                    string filename = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"imagenes\productos");
                    var extension = Path.GetExtension(files[0].FileName);
                    Producto productoDB = _unidadTrabajo.Producto.Obtener(productoVM.Producto.Id);
                    if (productoDB != null)
                        productoVM.Producto.ImageUrl = productoDB.ImageUrl;
                    if (productoVM.Producto.ImageUrl != null)
                    {
                        //Esto es para editar, necesitamos borrar la imagen anterior
                        var imagenPath = Path.Combine(webRootPath, productoVM.Producto.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(imagenPath))
                        {
                            System.IO.File.Delete(imagenPath);
                        }
                    }

                    using (var filesStreams = new FileStream(Path.Combine(uploads, filename + extension), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    productoVM.Producto.ImageUrl = @"\imagenes\productos\" + filename + extension;
                }
                else
                {
                    // Si en el Update el usuario no cambia la imagen
                    if (productoVM.Producto.Id != 0)
                    {
                        Producto productoDB = _unidadTrabajo.Producto.Obtener(productoVM.Producto.Id);
                        productoVM.Producto.ImageUrl = productoDB.ImageUrl;
                    }
                }


                if (productoVM.Producto.Id == 0)
                {
                    _unidadTrabajo.Producto.Agregar(productoVM.Producto);
                }
                else
                {
                    _unidadTrabajo.Producto.Actualizar(productoVM.Producto);
                }
                _unidadTrabajo.Guardar();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                productoVM.CategoriaLista = _unidadTrabajo.Categoria.ObtenerTodos().Select(c => new SelectListItem
                {
                    Text = c.Nombre,
                    Value = c.id.ToString()
                });
                productoVM.MarcaLista = _unidadTrabajo.Marca.ObtenerTodos().Select(m => new SelectListItem
                {
                    Text = m.Nombre,
                    Value = m.id.ToString()
                });
                productoVM.PadreLista = _unidadTrabajo.Producto.ObtenerTodos().Select(p => new SelectListItem
                {
                    Text = p.Descripcion,
                    Value = p.Id.ToString()
                });

                if (productoVM.Producto.Id != 0)
                {
                    productoVM.Producto = _unidadTrabajo.Producto.Obtener(productoVM.Producto.Id);
                }

            }
            return View(productoVM.Producto);
        }

        #region API
        [HttpGet]
        public IActionResult ObtenerTodos()
        {
            var todos = _unidadTrabajo.Producto.ObtenerTodos(IncluirPropiedades: "Categoria,Marca");
            return Json(new { data = todos });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var productoDb = _unidadTrabajo.Producto.Obtener(id);
            if (productoDb == null)
            {
                return Json(new { success = false, message = "Error al Borrar" });
            }
            // Eliminar la Imagen relacionada al producto
            string webRootPath = _hostEnvironment.WebRootPath;
            var imagenPath = Path.Combine(webRootPath, productoDb.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(imagenPath))
            {
                System.IO.File.Delete(imagenPath);
            }

            _unidadTrabajo.Producto.Remover(productoDb);
            _unidadTrabajo.Guardar();
            return Json(new { success = true, message = "Producto Borrado Exitosamente" });
        }

        #endregion
    }
}
