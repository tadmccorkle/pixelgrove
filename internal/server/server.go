package server

import (
	"context"
	"errors"
	"io"
	"log/slog"
	"math/rand/v2"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"

	"github.com/joho/godotenv"
	"golang.org/x/oauth2"
	"golang.org/x/oauth2/google"
)

type server struct {
	IsDev bool
	AuthConfig
}

const oauthRedirectPattern = "/auth/callback"

var conf = &oauth2.Config{
	RedirectURL: "http://localhost:4815" + oauthRedirectPattern,
	Scopes:      []string{"email", "profile"},
	Endpoint:    google.Endpoint,
}

func Serve() {
	err := godotenv.Load()
	if err != nil {
		slog.Error("failed to load .env", "error", err)
		os.Exit(1)
	}

	port := os.Getenv("PIXELGROVE_PORT")
	if port == "" {
		port = "4815"
	}

	var s server

	s.IsDev = os.Getenv("PIXELGROVE_ENV") == "dev"
	s.AuthConfig, err = NewAuth()

	mux := http.NewServeMux()

	mux.HandleFunc("/login", s.handlerLogin)
	// mux.HandleFunc("POST /login", s.handlerLogin)
	mux.HandleFunc("POST /logout", handlerLogout)
	mux.HandleFunc(oauthRedirectPattern, func(w http.ResponseWriter, r *http.Request) {
		slog.Info("method " + r.Method)
		code := r.URL.Query().Get("code")
		slog.Info("code " + code)
		token, err := conf.Exchange(context.Background(), code)
		if err != nil {
			slog.Error("Bad request", "error", err)
			http.Error(w, err.Error(), http.StatusBadRequest)
			return
		}
		slog.Info("refresh token " + token.RefreshToken)
		slog.Info("access token " + token.AccessToken)
		client := conf.Client(context.Background(), token)
		response, err := client.Get("https://www.googleapis.com/oauth2/v2/userinfo")
		if err != nil {
			slog.Error("Bad request", "error", err)
			http.Error(w, err.Error(), http.StatusBadRequest)
			return
		}
		defer response.Body.Close()
		body, err := io.ReadAll(response.Body)
		if err != nil {
			slog.Error("Bad request", "error", err)
			http.Error(w, err.Error(), http.StatusBadRequest)
			return
		}
		slog.Info(string(body))
		http.Redirect(w, r, "/", http.StatusTemporaryRedirect)
	})
	mux.HandleFunc("GET /events", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Add("Content-Type", "application/json")
		w.WriteHeader(200)
		if rand.IntN(2) == 0 {
			w.Write([]byte(`[
				{
					"id": 1,
					"name": "e1",
					"host": 1,
					"topic": "cheese"
				},
				{
					"id": 2,
					"name": "e1",
					"host": 1,
					"topic": "plants"
				},
				{
					"id": 3,
					"name": "event boi",
					"host": 3,
					"topic": "cars"
				},
				{
					"id": 4,
					"name": "abc",
					"host": 2,
					"topic": "letters"
				},
				{
					"id": 5,
					"name": "asdf",
					"host": 1,
					"topic": "keyboards"
				}
			]`))
		} else {
			w.Write([]byte("[]"))
		}
		slog.Info("responding to events!")
	})

	var client http.Handler

	if s.IsDev {
		webappPort := os.Getenv("PIXELGROVE_WEBAPP_DEV_PORT")
		if webappPort == "" {
			webappPort = "3000"
		}

		rp, err := url.Parse("http://localhost:" + webappPort)
		if err != nil {
			slog.Error("failed to set up client dev proxy", "error", err)
			os.Exit(1)
		}

		client = httputil.NewSingleHostReverseProxy(rp)
	} else {
		client = http.FileServer(http.Dir("./webapp/dist"))
	}

	mux.Handle("/", client)

	server := &http.Server{
		Handler: mux,
		Addr:    ":" + port,
	}

	slog.Info("server running at http://localhost" + server.Addr)

	if err := server.ListenAndServe(); err != http.ErrServerClosed {
		slog.Error("HTTP server error", "error", err)
		os.Exit(1)
	}
}

var ErrInvalidAuthEnv = errors.New("env is missing required auth credentials")

type AuthConfig struct {
	GoogleClientId     string
	GoogleClientSecret string
}

type login struct {
	Provider string `json:"provider"`
}

func NewAuth() (AuthConfig, error) {
	cfg := AuthConfig{
		GoogleClientId:     os.Getenv("PIXELGROVE_GOOGLE_CLIENT_ID"),
		GoogleClientSecret: os.Getenv("PIXELGROVE_GOOGLE_CLIENT_SECRET"),
	}

	conf.ClientID = cfg.GoogleClientId
	conf.ClientSecret = cfg.GoogleClientSecret

	if cfg.GoogleClientId == "" || cfg.GoogleClientSecret == "" {
		return cfg, ErrInvalidAuthEnv
	}

	return cfg, nil
}

// {
//   "id": "111261313293035164056",
//   "email": "tadmccorkle@gmail.com",
//   "verified_email": true,
//   "name": "Tad McCorkle",
//   "given_name": "Tad",
//   "family_name": "McCorkle",
//   "picture": "https://lh3.googleusercontent.com/a/ACg8ocI_4U5G3NGVKlTo5DUwxNVRl1m6CjPNuj6KLfA_3SjqTF3Q8Q=s96-c"
// }

func (s *server) handlerLogin(w http.ResponseWriter, r *http.Request) {
	// decoder := json.NewDecoder(r.Body)
	// var login login

	// err := decoder.Decode(&login)
	// if err != nil {
	//
	// }
	//
	// w.Header().Add("Content-Type", "application/json")
	// w.WriteHeader(200)
	// w.Write([]byte(`{ "user": "tad" }`))

	url := conf.AuthCodeURL("state", oauth2.AccessTypeOffline)
	http.Redirect(w, r, url, http.StatusTemporaryRedirect)
}

func handlerLogout(w http.ResponseWriter, r *http.Request) {
	w.Header().Add("Content-Type", "application/json")
	w.WriteHeader(200)
	w.Write([]byte(`{ "user": "tad" }`))
}
