using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Text;

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
            return StatusCode(201, "El grafo de productos ha sido creado exitosamente");
        }

        [HttpPost("initProductsRelations")]
        public async Task<IActionResult> InitProductsRelations()
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH (p:Productos)\nUNWIND p.marca as nombreMarcas\nMATCH (m:Marcas {nombre:nombreMarcas})\nCREATE (p)-[r:elaborado_por]->(m)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se creo correctamente la relacion producto-elaborado_por->marca");
        }

        [HttpPost("createProduct")]
        public async Task<IActionResult> CreateProduct(int idMarca, int idProducto, string nombreProducto, string nombreMarca, int precio)
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH(m: Marcas {id: " + idMarca + "})\nCREATE (p:Productos {id : " + idProducto + ", nombre : '" + nombreProducto + "', marca : '" + nombreMarca + "', precio : " + precio + "})\nCREATE (p)-[r:elaborado_por]->(m)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se creo el nuevo producto correctamente");
        }

        [HttpPut("modifyProduct")]
        public async Task<IActionResult> ModifyProduct(int idMarca, int idProducto, string nombreProducto, string nombreMarca, int precio)
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH(p:Productos {id: "+idProducto+"})-[r:elaborado_por]-()\nMATCH(m:Marcas {id: "+idMarca+"})\nset p = {id : "+idProducto+", nombre : '"+nombreProducto+"', marca : '"+nombreMarca+"', precio : "+precio+"}\nCREATE (p)-[r2:elaborado_por]->(m)\nDELETE r");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se modificó el producto correctamente");
        }
    }
}

