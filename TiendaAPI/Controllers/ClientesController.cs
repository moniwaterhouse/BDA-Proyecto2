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
    public class ClientesController : Controller
    {
        private readonly IDriver _driver;

        public ClientesController()
        {
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "1234"));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNode(string csvFilePath)
        {
            var statementText = new StringBuilder();
            statementText.Append("LOAD CSV WITH HEADERS FROM 'file:///" + csvFilePath + "' AS row\nWITH row WHERE row.id IS NOT NULL\nMERGE (c:Cliente {id: row.id, first_name: row.first_name, last_name: row.last_name})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "The clients graph has been succesfully created");
        }
    }
}

