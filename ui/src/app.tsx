import { type RouteDefinition, Router } from "@solidjs/router";
import { FileRoutes } from "@solidjs/start/router";
import { Meta, MetaProvider } from "@solidjs/meta";
import { createEffect, Suspense } from "solid-js";
import { querySession } from "./auth";
import Auth from "./components/Context";
import Nav from "./components/Nav";
import ErrorNotification from "./components/Error";
import { language, t } from "~/i18n";
import "./app.css";

export const route: RouteDefinition = {
  preload: ({ location }) => querySession(location.pathname)
};

export default function App() {
  createEffect(() => {
    if (typeof document !== "undefined") {
      document.documentElement.lang = language();
    }
  });

  return (
    <Router
      root={props => (
        <MetaProvider>
          <Meta name="description" content={t("meta.description")} />
          <Auth>
            <Suspense>
              <Nav />
              {props.children}
              <ErrorNotification />
            </Suspense>
          </Auth>
        </MetaProvider>
      )}
    >
      <FileRoutes />
    </Router>
  );
}
