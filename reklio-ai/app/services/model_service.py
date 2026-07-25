import json
import pickle

import pandas as pd
import xgboost as xgb

from ..config import FEATURE_NAMES, META_PATH, MODEL_PATH


class ModelService:
    """Učitava model, čuva redoslijed feature-a (feature_names_in_) i prag.
    Startup asertacija + reorder-po-imenu su srž T5.7 safeguard-a."""

    def __init__(self):
        self.model = None
        self.feature_names: list[str] = []
        self.threshold: float | None = None

    def load(self):
        with open(MODEL_PATH, "rb") as f:
            self.model = pickle.load(f)

        # Startup asertacija: model MORA imati imena (fit na DataFrame — T5.5),
        # i ona se moraju poklapati sa kanonskom listom. Inače glasno pukni.
        if not hasattr(self.model, "feature_names_in_"):
            raise RuntimeError(
                "Model nema feature_names_in_ — treniran na numpy nizu umjesto "
                "pandas DataFrame-a. Reorder po imenu je nemoguć. Ponovi trening.")

        self.feature_names = list(self.model.feature_names_in_)
        expected, got = set(FEATURE_NAMES), set(self.feature_names)
        if expected != got:
            raise RuntimeError(
                f"Imena feature-a se ne poklapaju sa modelom. "
                f"Fali u modelu: {expected - got}. Višak u modelu: {got - expected}.")

        with open(META_PATH, encoding="utf-8") as f:
            self.threshold = json.load(f).get("threshold")

    def score(self, payload: dict) -> tuple[float, list[str]]:
        # Reorder po IMENU — model.feature_names_in_ je izvor istine za redoslijed,
        # bez obzira kojim redom su brojevi stigli. Ako neki fali → KeyError (glasno).
        try:
            frame = pd.DataFrame([payload])[self.feature_names].astype(float)
        except KeyError as err:
            raise ValueError(f"Nedostaje feature: {err}") from err

        risk = float(self.model.predict_proba(frame)[0, 1])

        # Per-instance objašnjenje (SHAP doprinosi) — top faktori koji dižu rizik.
        contribs = self.model.get_booster().predict(
            xgb.DMatrix(frame), pred_contribs=True)[0]
        ranked = sorted(zip(self.feature_names, contribs[:-1]),
                        key=lambda pair: pair[1], reverse=True)
        top_factors = [name for name, contrib in ranked[:3] if contrib > 0]

        return risk, top_factors


model_service = ModelService()
