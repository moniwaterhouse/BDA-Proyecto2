using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Text;
using HR.Models;
using Newtonsoft.Json;
using TiendaAPI.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TiendaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : Controller
    {
        private readonly IDriver _driver;

        public ComprasController()
        {
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "1234"));
        }

        [HttpPost("loadCSV")]
        public async Task<IActionResult> LoadPurchasesCSV(string csvFilePath)
        {
            var statementText = new StringBuilder();
            statementText.Append("LOAD CSV WITH HEADERS FROM 'file:///" + csvFilePath + "' AS row\nWITH row WHERE row.idProducto IS NOT NULL\nMERGE (c:Compras {idCliente: toInteger(row.idCliente), idProducto : toInteger(row.idProducto), cantidad : toInteger(row.cantidad)})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "El grafo de compras ha sido creado exitosamente");
        }

        [HttpPost("initComprasRelations")]
        public async Task<IActionResult> InitComprasRelations()
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH (c:Productos)\nUNWIND c.id as productosIds\nMATCH (p:Compras {idProducto:productosIds})\nCREATE (p)-[r:contiene]->(c)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpPost("registrarCompra")]
        public async Task<IActionResult> RegistrarCompra(int idCliente, int idProducto, int cantidad)
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH(cliente:Clientes {id : "+idCliente+"})\nMATCH(producto:Productos {id : "+idProducto+"})\nCREATE (compra:Compras {idCliente : "+idCliente+", idProducto : "+idProducto+", cantidad : "+cantidad+"})\nCREATE (cliente)-[r:realizo]->(compra)\nCREATE (compra)-[r2: contiene]->(producto)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpGet("getAllCompras")]
        public async Task<IActionResult> GetAllCompras()
        {


            IResultCursor cursor;
            var resultados = new List<INode>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var compras = new List<Compra>();
            try
            {
                cursor = await session.RunAsync(@"MATCH (c:Compras) RETURN c");
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
                compras.Add(JsonConvert.DeserializeObject<Compra>(nodeProps));
            }
            return Ok(compras);
        }
    }
}

