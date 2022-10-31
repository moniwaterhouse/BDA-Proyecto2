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

        [HttpPost]
        public async Task<IActionResult> LoadProductsCSV(string csvFilePath)
        {
            var statementText = new StringBuilder();
            statementText.Append("LOAD CSV WITH HEADERS FROM 'file:///" + csvFilePath + "' AS row\nWITH row WHERE row.id IS NOT NULL\nMERGE (p:Productos {id: row.id, nombre : row.nombre, marca : row.marca, precio : row.precio})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "El grafo de productos ha sido creado exitosamente");
        }
    }
}

