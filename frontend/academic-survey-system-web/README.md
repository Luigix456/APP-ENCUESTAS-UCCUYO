# Academic Survey System Web

Frontend público para responder encuestas académicas desde un enlace o código QR.

## Tecnología

- React
- Vite
- TypeScript

## Flujo público

La pantalla de respuesta se abre en:

```bash
/survey/{accessCode}
```

El frontend consume la API con rutas relativas `/api/...`. En desarrollo, Vite redirige esas llamadas a `http://localhost:5047`.

## Área privada

Rutas iniciales:

```bash
/login
/app
```

La sesión autenticada usa JWT Bearer emitido por el backend. El token y su vencimiento se guardan en `sessionStorage` bajo una clave centralizada y la sesión se restaura consultando `GET /api/auth/me`.

## Comandos

```bash
npm install
npm run dev
npm run build
```

## Alcance actual

Incluye el flujo público para estudiantes y una primera base de autenticación privada con login, restauración de sesión, protección de `/app` y logout. No incluye dashboard real, gestión de sesiones, QR, usuarios, catálogo, resultados ni reportes.
