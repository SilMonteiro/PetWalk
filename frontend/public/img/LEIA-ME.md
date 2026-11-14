# Fotos do PetWalk

A landing page e as telas de login usam fotografias reais, que precisam ser
colocadas **nesta pasta** (`frontend/public/img/`). Enquanto o arquivo não existir,
a tela mostra um bloco colorido com uma patinha no lugar, sem quebrar o layout.

| Arquivo | Onde aparece | O que procurar |
| :-- | :-- | :-- |
| `hero-cachorro.jpg` | Hero da página inicial (destaque principal) | Cachorro grande, bem iluminado, olhando para a câmera, fundo desfocado. Formato quase quadrado (ex.: 1200×1300). |
| `passeador-parque.jpg` | Seção "Encontre seu passeador" | Passeador(a) caminhando com um ou mais cães em parque ou rua arborizada. |
| `caes-brincando.jpg` | Galeria "Para quem ama seus pets" (foto grande, vertical) | Cães brincando em parque ou gramado. |
| `tutor-e-pet.jpg` | Galeria "Para quem ama seus pets" | Tutor(a) abraçando ou brincando com o pet. |
| `gato.jpg` | Galeria "Para quem ama seus pets" | Gato em boa luz natural. |
| `passeador.jpg` | Seção "Para passeadores" | Passeador(a) sorrindo com um cachorro na coleira. |
| `login-cachorro.jpg` | Painel lateral do login e do cadastro | Cachorro em enquadramento vertical ou quadrado. |

## Dicas

- Use fotos com licença gratuita para uso comercial (Unsplash, Pexels ou Pixabay) e confira a licença de cada uma.
- Prefira JPG entre 100 KB e 400 KB. Redimensione para, no máximo, 1600 px no lado maior.
- Mantenha os nomes exatamente como na tabela. Se usar outro nome ou formato (por exemplo `.webp`), ajuste o caminho em `src/pages/LandingPage.jsx` e `src/components/AuthLayout.jsx`.
- Fotos de pessoas: use apenas imagens com autorização de uso de imagem (os bancos acima já informam isso).
