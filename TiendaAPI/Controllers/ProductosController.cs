using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Text;
using Newtonsoft.Json;
using TiendaAPI.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TiendaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : Controller
    {
        private readonly IDriver _driver;

        public ProductosController()
        {
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "1234"));
        }

        [HttpPost("loadCSV")]
        public async Task<IActionResult> LoadProductsCSV(string csvFilePath)
        {
            var statementText = new StringBuilder();
            statementText.Append("LOAD CSV WITH HEADERS FROM 'file:///" + csvFilePath + "' AS row\nWITH row WHERE row.id IS NOT NULL\nMERGE (p:Productos {id: toInteger(row.id), nombre : row.nombre, marca : row.marca, precio : toInteger(row.precio)})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpPost("initProductsRelations")]
        public async Task<IActionResult> InitProductsRelations()
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH (p:Productos)\nUNWIND p.marca as nombreMarcas\nMATCH (m:Marcas {nombre:nombreMarcas})\nCREATE (p)-[r:elaborado_por]->(m)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpPost("createProduct")]
        public async Task<IActionResult> CreateProduct(int idMarca, int idProducto, string nombreProducto, string nombreMarca, int precio)
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH(m: Marcas {id: " + idMarca + "})\nCREATE (p:Productos {id : " + idProducto + ", nombre : '" + nombreProducto + "', marca : '" + nombreMarca + "', precio : " + precio + "})\nCREATE (p)-[r:elaborado_por]->(m)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpPut("modifyProduct")]
        public async Task<IActionResult> ModifyProduct(int idMarca, int idProducto, string nombreProducto, string nombreMarca, int precio)
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH(p:Productos {id: "+idProducto+"})-[r:elaborado_por]-()\nMATCH(m:Marcas {id: "+idMarca+"})\nset p = {id : "+idProducto+", nombre : '"+nombreProducto+"', marca : '"+nombreMarca+"', precio : "+precio+"}\nCREATE (p)-[r2:elaborado_por]->(m)\nDELETE r");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpDelete("deleteProduct")]
        public async Task<IActionResult> DeleteProduct(int idProducto)
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH (p:Productos {id:"+idProducto+"})-[r:elaborado_por]-()\nOPTIONAL MATCH (m:Compras {idProducto:"+idProducto+"})-[r2:contiene]-()\nOPTIONAL MATCH ()-[r3:realizo]-(m)\nDELETE r2, r, p, m, r3");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpGet("getAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {


            IResultCursor cursor;
            var resultados = new List<INode>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var productos = new List<Producto>();
            try
            {
                cursor = await session.RunAsync(@"MATCH (p:Productos) RETURN p");
                resultados = await cursor.ToListAsync(record =>
                    record[0].As<INode>());

            }
            finally
            {
                await session.CloseAsync();
            }

            foreach (var result in resultados)
            {
                var nodeProps = JsonConvert.SerializeObject(result.As<INode>().Properties);
                productos.Add(JsonConvert.DeserializeObject<Producto>(nodeProps));
            }
            return Ok(productos);
        }

        [HttpGet("getProductosMasVendidos")]
        public async Task<IActionResult> GetProductosMasVendidos()
        {


            IResultCursor cursor;
            var resultados = new List<IRecord>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var productos = new List<ProductoCantidad>();
            try
            {
                cursor = await session.RunAsync(@"match (compra:Compras)-[r:contiene]-(producto:Productos)
                                                  return producto.nombre as nombre, sum(compra.cantidad) as unidadesVendidas
                                                  order by unidadesVendidas desc limit 5");
                resultados = await cursor.ToListAsync(record =>
                    record.As<IRecord>());

            }
            finally
            {
                await session.CloseAsync();
            }

            foreach (var result in resultados)
            {
                var props = JsonConvert.SerializeObject(result.As<IRecord>().Values);
                productos.Add(JsonConvert.DeserializeObject<ProductoCantidad>(props));

            }
            return Ok(productos);
        }
    }
}

