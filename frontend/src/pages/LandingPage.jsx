import { useState } from 'react';
import { Link } from 'react-router-dom';
import Foto from '../components/Foto';
import Icon from '../components/Icon';
import Logo from '../components/Logo';
import useReveal from '../hooks/useReveal';
import { useAuth } from '../context/AuthContext';
import '../styles/landing.css';

const PASSOS = [
  {
    icone: 'pata',
    titulo: 'Cadastre seu pet',
    texto: 'Informe nome, raça, porte, idade e observações importantes.'
  },
  {
    icone: 'search',
    titulo: 'Encontre um passeador',
    texto: 'Compare média de avaliações, valor por hora e tipo de cobrança.'
  },
  {
    icone: 'send',
    titulo: 'Solicite o passeio',
    texto: 'Escolha data, horário, duração e local. O valor é calculado ao solicitar.'
  },
  {
    icone: 'clock',
    titulo: 'Acompanhe o andamento',
    texto: 'Veja cada etapa do passeio e receba notificações a cada mudança.'
  },
  {
    icone: 'star',
    titulo: 'Avalie a experiência',
    texto: 'Depois do passeio finalizado, dê uma nota e conte como foi.'
  }
];

const BENEFICIOS = [
  {
    icone: 'star',
    tom: 'ambar',
    titulo: 'Avaliações de quem já passeou',
    texto: 'Cada passeador exibe a média das avaliações recebidas de tutores.'
  },
  {
    icone: 'wallet',
    tom: 'verde',
    titulo: 'Valor claro',
    texto: 'O passeador define o valor por hora e a plataforma calcula o total da solicitação.'
  },
  {
    icone: 'list',
    tom: 'lilas',
    titulo: 'Status em cada etapa',
    texto: 'Solicitado, aceito, em andamento, finalizado: você sempre sabe onde o passeio está.'
  },
  {
    icone: 'bell',
    tom: 'azul',
    titulo: 'Notificações',
    texto: 'Avisos quando um passeio é solicitado, aceito, iniciado, finalizado ou cancelado.'
  },
  {
    icone: 'shield',
    tom: 'marrom',
    titulo: 'Dados preservados',
    texto: 'E-mail e telefone não aparecem no perfil público. O local é o que você informa no passeio.'
  },
  {
    icone: 'heart',
    tom: 'rosa',
    titulo: 'Cuidado com o seu pet',
    texto: 'Porte e observações do pet ficam cadastrados para orientar cada passeio.'
  }
];

const PERGUNTAS = [
  {
    p: 'Como solicito um passeio?',
    r: 'Cadastre seu pet, escolha um passeador na lista e informe data, horário, duração e local do passeio. O passeador recebe a solicitação e pode aceitar ou recusar.'
  },
  {
    p: 'Como funciona o valor?',
    r: 'Cada passeador define o valor por hora e o tipo de cobrança: por hora ou por hora com adicional para porte grande. O valor total é calculado pela plataforma no momento da solicitação.'
  },
  {
    p: 'Posso cancelar um passeio?',
    r: 'Sim. O cancelamento fica disponível enquanto o passeio está solicitado ou aceito. Depois que o passeio começa, ele segue até ser finalizado.'
  },
  {
    p: 'Como acompanho o andamento?',
    r: 'Na página Meus passeios, cada passeio mostra o status atual em uma linha do tempo. Você também recebe notificações quando algo muda.'
  },
  {
    p: 'Posso ser tutor e passeador na mesma conta?',
    r: 'Pode. No cadastro, escolha o perfil “Ambos”. Você gerencia seus pets e também atende solicitações de passeios.'
  },
  {
    p: 'Quando posso avaliar um passeador?',
    r: 'Depois que o passeio é finalizado. Você escolhe uma nota de 1 a 5 e pode deixar um comentário.'
  }
];

export default function LandingPage() {
  const { usuario } = useAuth();
  const [menuAberto, setMenuAberto] = useState(false);

  useReveal();

  const fechar = () => setMenuAberto(false);

  return (
    <div className="landing">
      {/* ---------- Navegação ---------- */}
      <header className="lp-nav">
        <div className="container lp-nav-interno">
          <Logo altura={46} />

          <nav
            id="menu-landing"
            className={`lp-menu${menuAberto ? ' aberto' : ''}`}
            aria-label="Seções da página"
          >
            <a href="#como-funciona" onClick={fechar}>Como funciona</a>
            <a href="#tutores" onClick={fechar}>Para tutores</a>
            <a href="#passeadores" onClick={fechar}>Para passeadores</a>
            <a href="#beneficios" onClick={fechar}>Benefícios</a>
            <a href="#duvidas" onClick={fechar}>Dúvidas</a>
          </nav>

          <div className="lp-nav-acoes">
            {usuario ? (
              <Link className="btn btn--sm" to="/home">
                Ir para o painel
              </Link>
            ) : (
              <>
                <Link className="btn btn--ghost btn--sm lp-entrar" to="/login">
                  Entrar
                </Link>
                <Link className="btn btn--sm" to="/cadastro">
                  Cadastre-se
                </Link>
              </>
            )}

            <button
              type="button"
              className="lp-hamburguer"
              aria-expanded={menuAberto}
              aria-controls="menu-landing"
              aria-label={menuAberto ? 'Fechar menu' : 'Abrir menu'}
              onClick={() => setMenuAberto((aberto) => !aberto)}
            >
              <Icon nome={menuAberto ? 'x' : 'list'} />
            </button>
          </div>
        </div>
      </header>

      {/* ---------- Hero ---------- */}
      <section className="lp-hero">
        <span className="lp-hero-forma lp-hero-forma--1" aria-hidden="true" />
        <span className="lp-hero-forma lp-hero-forma--2" aria-hidden="true" />

        <div className="container lp-hero-grade">
          <div className="lp-hero-texto">
            <span className="pill lp-hero-selo">
              <Icon nome="pata" />
              Passeios com pets, do jeito certo
            </span>

            <h1>
              Mais passeios,
              <br />
              <span className="lp-destaque">mais felicidade!</span>
            </h1>

            <p>
              Encontre passeadores de confiança e proporcione momentos especiais para quem faz
              parte da sua família.
            </p>

            <div className="lp-hero-botoes">
              <Link className="btn btn--lg" to={usuario ? '/home' : '/cadastro'}>
                {usuario ? 'Ir para o painel' : 'Começar agora'}
                <Icon nome="arrow" />
              </Link>
              <a className="btn btn--lg btn--outline" href="#como-funciona">
                Conhecer a plataforma
              </a>
            </div>
          </div>

          <div className="lp-hero-visual">
            <div className="lp-hero-foto">
              <Foto
                src="/img/hero-cachorro.jpg"
                alt="Cachorro sorrindo em um parque"
                posicao="center 30%"
              />
            </div>

            <div className="lp-flutuante lp-flutuante--a" aria-hidden="true">
              <span className="lp-ponto lp-ponto--verde" />
              <div>
                <strong>Passeio em andamento</strong>
                <small>Acompanhe cada etapa</small>
              </div>
            </div>

            <div className="lp-flutuante lp-flutuante--b" aria-hidden="true">
              <span className="lp-flutuante-icone lp-flutuante-icone--azul">
                <Icon nome="calendar" />
              </span>
              <div>
                <strong>Passeio aceito</strong>
                <small>Tudo confirmado</small>
              </div>
            </div>

            <div className="lp-flutuante lp-flutuante--c" aria-hidden="true">
              <span className="lp-flutuante-icone lp-flutuante-icone--ambar">
                <Icon nome="star" />
              </span>
              <div>
                <strong>Avalie o passeador</strong>
                <small>Nota de 1 a 5</small>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ---------- Como funciona ---------- */}
      <section id="como-funciona" className="lp-secao">
        <div className="container">
          <div className="lp-titulo revelar">
            <span className="lp-sobretitulo">Como funciona</span>
            <h2>Do cadastro ao passeio em cinco passos</h2>
            <p>Simples para o tutor, organizado para o passeador.</p>
          </div>

          <ol className="lp-passos">
            {PASSOS.map((passo, indice) => (
              <li
                key={passo.titulo}
                className="lp-passo revelar"
                style={{ transitionDelay: `${indice * 80}ms` }}
              >
                <span className="lp-passo-numero">{indice + 1}</span>
                <span className="lp-passo-icone">
                  <Icon nome={passo.icone} />
                </span>
                <h3>{passo.titulo}</h3>
                <p>{passo.texto}</p>
              </li>
            ))}
          </ol>
        </div>
      </section>

      {/* ---------- Encontre seu passeador ---------- */}
      <section className="lp-secao lp-secao--verde">
        <div className="container lp-divisao">
          <div className="lp-divisao-foto revelar">
            <Foto
              src="/img/passeador-parque.jpg"
              alt="Passeador caminhando com cães em um parque"
            />
          </div>

          <div className="lp-divisao-texto revelar">
            <span className="lp-sobretitulo lp-sobretitulo--claro">Encontre seu passeador</span>
            <h2>Quem passeia com seu pet, a poucos cliques</h2>
            <p>
              Veja a lista de passeadores ativos, compare as avaliações e o valor por hora e
              solicite o passeio sem sair da plataforma.
            </p>

            <ul className="lp-checks">
              <li><Icon nome="check" /> Média de avaliações de cada passeador</li>
              <li><Icon nome="check" /> Valor por hora e tipo de cobrança à vista</li>
              <li><Icon nome="check" /> Solicitação com data, horário e local</li>
              <li><Icon nome="check" /> Acompanhamento de cada etapa do passeio pelo status</li>
            </ul>

            <Link className="btn btn--branco btn--lg" to={usuario ? '/passeadores' : '/cadastro'}>
              Buscar passeadores
              <Icon nome="arrow" />
            </Link>
          </div>
        </div>
      </section>

      {/* ---------- Para quem ama seus pets ---------- */}
      <section id="tutores" className="lp-secao lp-secao--lilas">
        <div className="container">
          <div className="lp-titulo revelar">
            <span className="lp-sobretitulo">Para quem ama seus pets</span>
            <h2>Tranquilidade para você, alegria para eles</h2>
            <p>
              Seu pet merece passeios com carinho, atenção e quem entenda o jeitinho dele.
              Cadastre porte e observações para o passeador saber como cuidar.
            </p>
          </div>

          <div className="lp-galeria">
            <div className="lp-galeria-item lp-galeria-item--grande revelar">
              <Foto src="/img/caes-brincando.jpg" alt="Cães brincando juntos em um parque" />
            </div>
            <div className="lp-galeria-item revelar" style={{ transitionDelay: '100ms' }}>
              <Foto src="/img/tutor-e-pet.jpg" alt="Tutora abraçando seu cachorro" />
            </div>
            <div className="lp-galeria-item revelar" style={{ transitionDelay: '200ms' }}>
              <Foto src="/img/gato.jpg" alt="Gato de olhos atentos" />
            </div>
          </div>
        </div>
      </section>

      {/* ---------- Para passeadores ---------- */}
      <section id="passeadores" className="lp-secao lp-secao--creme">
        <div className="container lp-divisao lp-divisao--invertida">
          <div className="lp-divisao-texto revelar">
            <span className="lp-sobretitulo lp-sobretitulo--marrom">Para passeadores</span>
            <h2>Transforme o seu amor por pets em renda</h2>
            <p>
              Cadastre-se como passeador, defina o seu valor por hora e receba solicitações de
              tutores. Você aceita ou recusa, inicia e finaliza cada passeio pela plataforma.
            </p>

            <ul className="lp-checks lp-checks--escuro">
              <li><Icon nome="check" /> Você define o valor por hora</li>
              <li><Icon nome="check" /> Aceite ou recuse cada solicitação</li>
              <li><Icon nome="check" /> Notificações de novas solicitações</li>
              <li><Icon nome="check" /> Construa sua reputação com avaliações</li>
            </ul>

            <Link className="btn btn--lg" to={usuario ? '/home' : '/cadastro'}>
              Quero ser passeador
              <Icon nome="arrow" />
            </Link>
          </div>

          <div className="lp-divisao-foto lp-divisao-foto--marrom revelar">
            <Foto src="/img/passeador.jpg" alt="Passeador sorrindo com um cachorro na coleira" />
          </div>
        </div>
      </section>

      {/* ---------- Benefícios ---------- */}
      <section id="beneficios" className="lp-secao">
        <div className="container">
          <div className="lp-titulo revelar">
            <span className="lp-sobretitulo">Benefícios e diferenciais</span>
            <h2>Tudo o que um bom passeio precisa</h2>
          </div>

          <div className="lp-beneficios">
            {BENEFICIOS.map((item, indice) => (
              <article
                key={item.titulo}
                className="lp-beneficio revelar"
                style={{ transitionDelay: `${(indice % 3) * 90}ms` }}
              >
                <span className={`lp-beneficio-icone lp-beneficio-icone--${item.tom}`}>
                  <Icon nome={item.icone} />
                </span>
                <h3>{item.titulo}</h3>
                <p>{item.texto}</p>
              </article>
            ))}
          </div>
        </div>
      </section>

      {/* ---------- FAQ ---------- */}
      <section id="duvidas" className="lp-secao lp-secao--lilas">
        <div className="container lp-faq-grade">
          <div className="lp-titulo lp-titulo--esquerda revelar">
            <span className="lp-sobretitulo">Perguntas frequentes</span>
            <h2>Ficou com alguma dúvida?</h2>
            <p>Reunimos as respostas sobre como o PetWalk funciona.</p>
          </div>

          <div className="lp-faq revelar">
            {PERGUNTAS.map((item) => (
              <details key={item.p} className="lp-faq-item">
                <summary>
                  {item.p}
                  <Icon nome="chevron" />
                </summary>
                <p>{item.r}</p>
              </details>
            ))}
          </div>
        </div>
      </section>

      {/* ---------- Chamada final ---------- */}
      <section className="lp-cta">
        <div className="container">
          <div className="lp-cta-cartao revelar">
            <Icon nome="pata" className="lp-cta-pata" />
            <h2>Pronto para o próximo passeio?</h2>
            <p>Crie sua conta e comece a organizar os passeios do seu pet.</p>
            <div className="lp-hero-botoes lp-hero-botoes--centro">
              <Link className="btn btn--branco btn--lg" to={usuario ? '/home' : '/cadastro'}>
                {usuario ? 'Ir para o painel' : 'Criar minha conta'}
              </Link>
              {!usuario && (
                <Link className="btn btn--lg lp-cta-entrar" to="/login">
                  Já tenho conta
                </Link>
              )}
            </div>
          </div>
        </div>
      </section>

      {/* ---------- Rodapé ---------- */}
      <footer className="lp-rodape">
        <div className="container lp-rodape-grade">
          <div className="lp-rodape-marca">
            <span className="lp-rodape-logo">
              <Logo altura={52} />
            </span>
            <p>Plataforma web para intermediação de passeios com pets.</p>
          </div>

          <nav aria-label="Navegação do rodapé">
            <h3>Navegação</h3>
            <a href="#como-funciona">Como funciona</a>
            <a href="#tutores">Para tutores</a>
            <a href="#passeadores">Para passeadores</a>
            <a href="#duvidas">Perguntas frequentes</a>
          </nav>

          <nav aria-label="Acesso">
            <h3>Acesso</h3>
            <Link to="/login">Entrar</Link>
            <Link to="/cadastro">Cadastre-se</Link>
          </nav>
        </div>

        <div className="container lp-rodape-fim">
          <p>© {new Date().getFullYear()} PetWalk. Mais passeios, mais felicidade!</p>
        </div>
      </footer>
    </div>
  );
}
