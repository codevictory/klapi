import { redirect } from "@solidjs/router";
import { useSession } from "vinxi/http";
import { getRandomValues, subtle, timingSafeEqual } from "crypto";
import { createUser, findUser } from "./db";
import type { Language } from "~/i18n";
import { getTranslations } from "~/i18n";

export interface Session {
  id: number;
  email: string;
}

export const getSession = () =>
  useSession<Session>({
    password: process.env.SESSION_SECRET!,
  });

export async function createSession(user: Session, redirectTo?: string) {
  const validDest = redirectTo?.[0] === "/" && redirectTo[1] !== "/";
  const session = await getSession();
  await session.update(user);
  return redirect(validDest ? redirectTo : "/");
}

async function createHash(password: string) {
  const salt = getRandomValues(new Uint8Array(16));
  const saltHex = Buffer.from(salt).toString("hex");
  const key = await subtle.deriveBits(
    {
      name: "PBKDF2",
      salt,
      iterations: 100_000,
      hash: "SHA-512",
    },
    await subtle.importKey(
      "raw",
      new TextEncoder().encode(password),
      "PBKDF2",
      false,
      ["deriveBits"],
    ),
    512,
  );
  const hash = Buffer.from(key).toString("hex");
  return `${saltHex}:${hash}`;
}

async function checkPassword(
  storedPassword: string,
  providedPassword: string,
  lang: Language,
) {
  const translations = getTranslations(lang);
  const [storedSalt, storedHash] = storedPassword.split(":");
  if (!storedSalt || !storedHash)
    throw new Error(translations["errors.invalidStoredPasswordFormat"]);
  const key = await subtle.deriveBits(
    {
      name: "PBKDF2",
      salt: Buffer.from(storedSalt, "hex"),
      iterations: 100_000,
      hash: "SHA-512",
    },
    await subtle.importKey(
      "raw",
      new TextEncoder().encode(providedPassword),
      "PBKDF2",
      false,
      ["deriveBits"],
    ),
    512,
  );
  const hash = Buffer.from(key);
  const stored = Buffer.from(storedHash, "hex");
  if (stored.length !== hash.length || !timingSafeEqual(stored, hash))
    throw new Error(translations["errors.invalidEmailOrPassword"]);
}

export async function passwordLogin(
  email: string,
  password: string,
  lang: Language = "en",
) {
  const translations = getTranslations(lang);
  let user = await findUser({ email });
  if (!user)
    user = await createUser({
      email,
      password: await createHash(password),
    });
  else if (!user.password) throw new Error(translations["errors.oauthOnly"]);
  else await checkPassword(user.password, password, lang);
  return createSession(user);
}
