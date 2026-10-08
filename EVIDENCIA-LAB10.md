# Evidencia Lab 10 — Integración de cuatro ramas paralelas

Estado al 2026-10-07: `master` en `123ff0d`. Las cuatro ramas de los worktrees están integradas. No hay push.

## 1. Historial de master

`git log --oneline --graph master` (desde el punto de partida `ff5aae9`):

```text
* 123ff0d chore: tighten agent git permissions and ignore course content
* af27be2 fix(contact): reject contact requests about inactive products
*   1cfc444 merge: integrate worktree-contacto into master
|\
| * 977efd1 feat(contact): add contact requests behind the Features:Contact flag
* |   74f689d merge: integrate worktree-inicio into master
|\ \
| * | d93a493 feat(home): add summary with active products, stock and latest purchases
| |/
* |   7a7913b merge: integrate worktree-compras into master
|\ \
| * | d598cb2 feat(purchases): register purchases with stock validation
| |/
* |   87ba62f merge: integrate worktree-productos into master
|\ \
| * | ff79584 feat(products): add product creation and logical deactivation
| |/
* |   b8c8fd8 merge: integrate feat/products-purchases-pages into master
|\ \
| * | a6ed755 agentes y gaurdrails
| |/
| * c4bf116 chore: ignore agent worktrees and move database settings to user secrets   (tag: base)
| * 0946ff2 docs(odd): record repositories and pages progress
| * 9ef9bd2 docs: rewrite CLAUDE.md with commands, structure and rules
| * 58bb263 feat(pages): add products and purchases pages
| * 73b6970 feat(data): add repository contracts and EF Core repositories
| * 2f18fa7 docs(odd): add products and purchases pages feature document
| * 7c9d652 feat(data): add product IsActive, OperationResult and xUnit test project
|/
*   ff5aae9 Merge pull request #5 from 4l3x31s/feat/customers-products-purchases-seed
```

Las cuatro ramas de funcionalidad salen del tag `base` (`c4bf116`) y tienen un commit cada una.

## 2. Merges

Los archivos en conflicto se obtuvieron repitiendo cada merge con `git merge-tree --write-tree --name-only <padre1> <padre2>`. La resolución es la que registra el mensaje de cada commit de merge.

| # | Merge | Rama | Archivos en conflicto | Pruebas tras el merge |
|---|---|---|---|---|
| 0 | `b8c8fd8` | `feat/products-purchases-pages` (base) | Ninguno | 30 |
| 1 | `87ba62f` | `worktree-productos` | Ninguno | 73 |
| 2 | `7a7913b` | `worktree-compras` | `IProductRepository.cs`, `EfProductRepository.cs`, `Program.cs`, `EfProductRepositoryTests.cs` | 106 |
| 3 | `74f689d` | `worktree-inicio` | `IPurchaseRepository.cs`, `EfPurchaseRepository.cs`, `Program.cs`, `EfPurchaseRepositoryTests.cs`, `ServiceRegistrationTests.cs` | 121 |
| 4 | `1cfc444` | `worktree-contacto` | `Program.cs`, `Views/Home/Index.cshtml` | 163 |

### Merge 0 — base

Sin conflictos: `master` no tenía commits propios. Entra primero porque las cuatro ramas parten de ahí.

### Merge 1 — `worktree-productos`

Sin conflictos. Se integró primero porque es la única rama que rompe un contrato existente: `IProductRepository.GetAllAsync()` pasa a `GetAllAsync(bool includeInactive)`. Así las demás se adaptan una sola vez a la firma definitiva.

### Merge 2 — `worktree-compras`

Los cuatro conflictos se resolvieron conservando ambos lados, porque cada rama agregó miembros distintos en el mismo lugar:

- `IProductRepository.cs` y `EfProductRepository.cs`: alta y baja lógica (productos) junto a `UpdateStockAsync` (compras).
- `Program.cs`: se registran `ProductService` y `PurchaseService`.
- `EfProductRepositoryTests.cs`: se conservó el `using` de `Models` que necesitan las pruebas de alta y baja.

### Merge 3 — `worktree-inicio`

Mismo criterio, conservar ambos lados:

- `IPurchaseRepository.cs` y `EfPurchaseRepository.cs`: `AddAsync` (compras) junto a `GetLatestAsync` (inicio).
- `Program.cs`: quedan registrados `ProductService`, `PurchaseService` y `SummaryService`.
- `EfPurchaseRepositoryTests.cs` y `ServiceRegistrationTests.cs`: se conservan las pruebas de los dos lados.

### Merge 4 — `worktree-contacto`

- `Program.cs`: los servicios ya integrados junto al repositorio y el servicio de solicitudes de contacto.
- `Views/Home/Index.cshtml`: el modelo `Summary` de inicio junto a la configuración inyectada que oculta la tarjeta de Contact con el flag apagado.

La migración `AddContactRequests` viaja en este merge y no se aplicó aquí.

## 3. Conflictos semánticos

Git compara texto por archivo. No sabe que una línea que nadie tocó llama a un método cuya firma cambió en otro archivo. Estos casos no aparecieron como conflicto y solo se ven al compilar o al probar.

**a) Firma de `GetAllAsync` (merge 3).** `Services/SummaryService.cs` llegó de `worktree-inicio` llamando a `products.GetAllAsync()` sin argumento, contra el contrato de `base`. `worktree-productos` ya había cambiado la firma en `IProductRepository.cs`. Git no marcó nada porque `SummaryService.cs` es un archivo nuevo que solo existe en una rama: no hay dos versiones del mismo texto que comparar. Se adaptó a `GetAllAsync(includeInactive: false)` dentro del merge.

**b) Clases duplicadas en las pruebas (merges 2 y 4).** Productos, compras y contacto declararon cada una su propio `FakeProductRepository` y `FixedTimeProvider`, en archivos distintos. Archivos distintos no chocan para Git, pero dos clases con el mismo nombre en el mismo espacio de nombres no compilan. Se unificaron en una clase compartida por tipo.

**c) Dobles de prueba incompletos (merges 2, 3 y 4).** Cada rama escribió sus dobles contra las interfaces de `base`. Al crecer las interfaces, esos dobles dejaron de implementarlas. Se completaron en cada merge (`FakeRepositories.cs`, `SummaryServiceTests.cs`, `ContactRequestFakes.cs`).

**d) Regla de negocio cruzada (después del merge 4, `af27be2`).** Contacto validaba que el SKU existiera, pero no sabía que productos introducía la baja lógica. El código compilaba y las pruebas pasaban; el defecto era de comportamiento: se podía pedir contacto sobre un producto dado de baja. Se corrigió con su prueba en un commit aparte.

La evidencia de estos ajustes es la lista de archivos que en cada merge difieren de ambos padres sin haber estado en conflicto (`git diff-tree --cc --name-only <merge>`): `PurchasesController.cs`, `FakeProductRepository.cs`, `FakeRepositories.cs`, `SummaryService.cs`, `SummaryServiceTests.cs`, `ContactRequestFakes.cs`, `FixedTimeProvider.cs`, entre otros.

## 4. `dotnet test` del conjunto

Ejecutado sobre el árbol de trabajo el 2026-10-07:

```text
Con error! - Con error: 1, Superado: 164, Omitido: 0, Total: 165
```

- **Total:** 165. **En verde:** 164. **En rojo:** 1.
- La prueba en rojo es `ProductServiceTests.CreateWithNameWithoutLetters_IsRejected`. Se agregó después de la integración y está sin commit. Falla porque `ProductService` acepta el nombre "12345"; el servicio no se modificó por instrucción expresa.
- Lo que está en commit en `master` (`123ff0d`) corresponde a las otras 164 pruebas, todas en verde.

## 5. Informe de los revisores

Revisión de solo lectura de los 59 archivos de `git diff base...HEAD`, con `revisor-seguridad`, `revisor-estandar` y `revisor-pruebas` en paralelo. Los revisores no ejecutan git ni la suite.

**Bloqueante (1)**

- `EfContactRequestRepository` no tiene ninguna prueba (`CountByYearAsync` y `AddAsync`). El doble de prueba cuenta de otra forma, así que nada verifica la consulta que genera los folios reales.

**Mayores (5)**

- El folio de contacto es "conteo + 1" sin transacción ni reintento (`Services/ContactRequestService.cs:72`): dos solicitudes simultáneas chocan con el índice único y la segunda termina en error 500.
- Ningún POST a `/Contact` está probado por HTTP: ni el 404 con el flag apagado, ni el 400 sin token, ni el enlace de los campos del formulario.
- `EfUnitOfWork` no demuestra que un fallo revierte lo escrito.
- Las ramas de la vista de Contact (folio de éxito, errores, valores conservados) no tienen prueba.
- `.claude/settings.json` permite `Bash` y `PowerShell` completos y las denegaciones solo están escritas para Bash.

**Menores**

- `settings.json` apunta al hook `.claude/hooks/gate.ps1`, que no existe.
- `odd/tasks/products-purchases-pages.md:33` publica el host y el puerto de la base, sin contraseña.
- Pruebas que dependen del entorno Development o de datos sembrados (`SKU-00001`).
- Dobles de prueba duplicados y distintos del repositorio real.
- Ramas sin prueba: carrera al crear el mismo SKU, recorte de espacios en Contact, mensajes de éxito tras redirigir.
- No hay autenticación: es el diseño actual, no una regresión.

**Limpio**

- Ninguna credencial en archivos versionados ni en el historial de `appsettings*.json`.
- Estándar de datos sin incumplimientos: `HasMaxLength`, `HasPrecision(18, 2)`, índices únicos de SKU y folio, sin SQL crudo, baja lógica.
- Los cuatro POST llevan `[ValidateAntiForgeryToken]`; los registros de log no incluyen datos de clientes.
- Ninguna prueba escribe en la base sin revertir la transacción.

### Veredicto de `ci/veredicto-lab10.json`

**El archivo no existe.** No hay carpeta `ci/` en el árbol de trabajo ni en el historial de git, y el hook que `settings.json` declara (`.claude/hooks/gate.ps1`) tampoco existe. No hay veredicto automático que citar.

Veredicto que se desprende de la evidencia de este documento: **no aprobado todavía**, por un bloqueante de pruebas y una prueba en rojo.

## 6. Qué patrón habría evitado los choques

- **"Todo al final / debajo de `TimeProvider`".** `Program.cs` chocó en los seis pares de ramas y en tres de los cuatro merges, porque todas agregaron su registro en la misma línea. Lo mismo pasó al agregar métodos al final de las interfaces. Alternativa: que cada funcionalidad registre sus servicios en un método de extensión propio (`AddProducts()`, `AddPurchases()`), en su propio archivo, y que la base deje ya escritas esas llamadas. Cada rama edita entonces un archivo que nadie más toca.
- **Acordar contratos antes de paralelizar.** Los cuatro conflictos semánticos vienen de contratos que cambiaron durante el trabajo en paralelo. Si la base hubiera incluido la firma final de `GetAllAsync(bool includeInactive)`, los métodos nuevos de las dos interfaces y un único juego de dobles de prueba compartidos, las ramas solo habrían implementado. También habría hecho visible la regla "un producto inactivo no admite operaciones", que contacto no conocía.
- **Ramas más cortas.** Cada rama vivió como un solo commit de 500 a 1200 líneas e integró al final. Integrar primero la rama que cambia contratos y hacer que las demás se actualicen contra ella habría convertido cuatro merges con adaptación en cambios pequeños y aislados.
